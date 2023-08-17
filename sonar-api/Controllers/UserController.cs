using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using String = System.String;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Authorize(Policy = "AllowAnyScope")]
[Route("api/v{version:apiVersion}/user")]
public class UserController : ControllerBase {
  private readonly DataContext _dbContext;
  private readonly DbSet<User> _userTable;
  private readonly IPermissionsRepository _permissionsRepository;


  public UserController(
    DataContext dbContext,
    DbSet<User> userTable,
    IPermissionsRepository permissionsRepository) {

    this._dbContext = dbContext;
    this._userTable = userTable;
    this._permissionsRepository = permissionsRepository;
  }

  [HttpPost]
  [ProducesResponseType(typeof(CurrentUserView), statusCode: 201)]
  [ProducesResponseType(typeof(CurrentUserView), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  public async Task<ActionResult> UpdateCurrentUser(
    CancellationToken cancellationToken = default) {

    var principal = HttpContext.User;
    if (principal == null) {
      return this.Unauthorized(new {
        Status = "Unauthorized",
        Message = "Unauthorized to access requested resource."
      });
    }

    var userEmail = principal.FindFirstValue(ClaimTypes.Email);

    // return 400 (Bad Request) if claims are missing
    if (String.IsNullOrEmpty(userEmail)) {
      return this.BadRequest(new {
        Message = "Required claims are missing."
      });
    }

    var fullName = principal.FindFirstValue("name") ?? userEmail;

    await using var tx = await this._dbContext.Database
      .BeginTransactionAsync(cancellationToken);

    // Attempt to fetch existing user from db
    var existingUser = await this._userTable
      .Where(e => e.Email == userEmail)
      .SingleOrDefaultAsync(cancellationToken);

    CurrentUserView response;
    // check if user exists, update (if necessary) and return user
    if (existingUser != null) {
      // check if any user properties have changed
      // if so, update user entity and returned updated user.
      if (existingUser.FullName != fullName) {
        var updatedUser = this._userTable.Update(new User(
          existingUser.Id,
          existingUser.Email,
          fullName)
        );
        await this._dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        // get global admin status
        var isAdmin = await this._permissionsRepository.GetAdminStatus(existingUser.Id, cancellationToken);
        response = new CurrentUserView(
          updatedUser.Entity.FullName,
          updatedUser.Entity.Email,
          isAdmin);
      } else {
        var isAdmin = await this._permissionsRepository.GetAdminStatus(existingUser.Id, cancellationToken);
        response = new CurrentUserView(
          existingUser.FullName,
          existingUser.Email,
          isAdmin);
      }
    } else {
      // new user, create and return user obj
      var entity =
        await this._userTable.AddAsync(
          new User(Guid.Empty, userEmail, fullName),
          cancellationToken);

      await this._dbContext.SaveChangesAsync(cancellationToken);
      await tx.CommitAsync(cancellationToken);

      response = new CurrentUserView(
        entity.Entity.FullName,
        entity.Entity.Email,
        false
      );
    }

    return this.StatusCode(
      (Int32)HttpStatusCode.Created,
      response
    );
  }

  [HttpGet]
  [ProducesResponseType(typeof(CurrentUserView[]), statusCode: 200)]
  public async Task<ActionResult> GetUsers(
    CancellationToken cancellationToken = default) {

    var users = await this._userTable
      .ToListAsync(cancellationToken);

    var userViewList = new List<CurrentUserView>();
    foreach (var user in users) {
      var isAdmin = await this._permissionsRepository.GetAdminStatus(user.Id, cancellationToken);
      userViewList.Add(new CurrentUserView(user.FullName, user.Email, isAdmin));
    }

    return this.Ok(userViewList);
  }

  // Gets permission tree for the current user
  [HttpGet("permission-tree", Name = "GetUserPermissionTree")]
  [ProducesResponseType(typeof(UserPermissionsView), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetUserPermissionTree(
    CancellationToken cancellationToken) {

    var principal = HttpContext.User;
    if (principal == null) {
      return this.Unauthorized(new {
        Status = "Unauthorized",
        Message = "Unauthorized to access requested resource."
      });
    }

    var userEmail = principal.FindFirstValue(ClaimTypes.Email);

    // return 400 (Bad Request) if claims are missing
    if (String.IsNullOrEmpty(userEmail)) {
      return this.BadRequest(new {
        Message = "Required claims are missing."
      });
    }

    // Attempt to fetch existing user from db
    var existingUser = await this._userTable
      .Where(e => e.Email == userEmail)
      .SingleOrDefaultAsync(cancellationToken);

    if (existingUser == null) {
      return this.NotFound();
    }

    var result = await this._permissionsRepository.GetUserPermissionsView(existingUser.Id, cancellationToken);
    return this.Ok(result);
  }
}


