using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Authorize(Policy = "AllowAnyScope")]
[Route("api/v{version:apiVersion}/user")]
public class UserController : ControllerBase {
  private readonly DataContext _dbContext;
  private readonly DbSet<User> _userTable;

  public UserController(
    DataContext dbContext,
    DbSet<User> userTable) {

    this._dbContext = dbContext;
    this._userTable = userTable;
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

    var userEmail = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var firstName = principal.FindFirstValue("firstName");
    var lastName = principal.FindFirstValue("lastName");

    // return 400 (Bad Request) if claims are missing
    if (String.IsNullOrEmpty(userEmail) ||
      String.IsNullOrEmpty(firstName) ||
      String.IsNullOrEmpty(lastName)) {
      return this.BadRequest(new {
        Message = "Required claims are missing."
      });
    }

    await using var tx = await this._dbContext.Database
      .BeginTransactionAsync(cancellationToken);

    // Attempt to fetch existing user from db
    var existingUser = await this._userTable
      .Where(e => e.Email == userEmail)
      .SingleOrDefaultAsync(cancellationToken);

    // check if user exists, update (if necessary) and return user
    if (existingUser != null) {
      // check if any user properties have changed
      // if so, update user entity and returned updated user.
      if (existingUser.FirstName != firstName ||
        existingUser.LastName != lastName) {
        var updatedUser = this._userTable.Update(new User(
          existingUser.Id,
          existingUser.Email,
          firstName,
          lastName)
        );
        await this._dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return this.Ok(new CurrentUserView(
          updatedUser.Entity.FirstName,
          updatedUser.Entity.LastName,
          updatedUser.Entity.Email));
      }

      // no user properties have changed, return existing user
      return this.Ok(new CurrentUserView(
        existingUser.FirstName,
        existingUser.LastName,
        existingUser.Email));
    }

    // new user, create and return user obj
    var entity =
      await this._userTable.AddAsync(
        new User(Guid.Empty, userEmail, firstName, lastName),
        cancellationToken);

    await this._dbContext.SaveChangesAsync(cancellationToken);
    await tx.CommitAsync(cancellationToken);

    return this.StatusCode(
      (Int32)HttpStatusCode.Created,
      new CurrentUserView(entity.Entity.FirstName,
        entity.Entity.LastName,
        entity.Entity.Email));
  }

  [HttpGet]
  [ProducesResponseType(typeof(CurrentUserView[]), statusCode: 200)]
  public async Task<ActionResult> GetUsers(
    CancellationToken cancellationToken = default) {

    var users = await this._userTable
      .Select(user =>
        new CurrentUserView(user.FirstName, user.LastName, user.Email)
      )
      .ToListAsync(cancellationToken);
    return this.Ok(users);
  }
}
