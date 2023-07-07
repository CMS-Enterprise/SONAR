﻿// <auto-generated />
using System;
using Cms.BatCave.Sonar.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cms.BatCave.Sonar.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20230706192507_AddInitialSsoTables")]
    partial class AddInitialSsoTables
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:CollationDefinition:ci_collation", "en-u-ks-level2,en-u-ks-level2,icu,False")
                .HasAnnotation("ProductVersion", "7.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.ApiKey", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid?>("EnvironmentId")
                        .HasColumnType("uuid")
                        .HasColumnName("environment_id");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(74)
                        .HasColumnType("character varying(74)")
                        .HasColumnName("key")
                        .UseCollation("ci_collation");

                    b.Property<Guid?>("TenantId")
                        .HasColumnType("uuid")
                        .HasColumnName("tenant_id");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_api_key");

                    b.HasIndex("Key")
                        .HasDatabaseName("ix_api_key_key");

                    b.HasIndex("TenantId")
                        .HasDatabaseName("ix_api_key_tenant_id");

                    b.ToTable("api_key", (string)null);
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.Environment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name")
                        .UseCollation("ci_collation");

                    b.HasKey("Id")
                        .HasName("pk_environment");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasDatabaseName("ix_environment_name");

                    b.ToTable("environment", (string)null);
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.HealthCheck", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Definition")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("definition")
                        .UseCollation("ci_collation");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description")
                        .UseCollation("ci_collation");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name")
                        .UseCollation("ci_collation");

                    b.Property<Guid>("ServiceId")
                        .HasColumnType("uuid")
                        .HasColumnName("service_id");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_health_check");

                    b.HasIndex("ServiceId", "Name")
                        .IsUnique()
                        .HasDatabaseName("ix_health_check_service_id_name");

                    b.ToTable("health_check", (string)null);
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.HealthCheckCache", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("HealthCheck")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("health_check")
                        .UseCollation("ci_collation");

                    b.Property<Guid>("ServiceHealthId")
                        .HasColumnType("uuid")
                        .HasColumnName("service_health_id");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.HasKey("Id")
                        .HasName("pk_health_check_cache");

                    b.HasIndex("ServiceHealthId", "HealthCheck")
                        .IsUnique()
                        .HasDatabaseName("ix_health_check_cache_service_health_id_health_check");

                    b.ToTable("health_check_cache", (string)null);
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.Service", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description")
                        .UseCollation("ci_collation");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("display_name")
                        .UseCollation("ci_collation");

                    b.Property<bool>("IsRootService")
                        .HasColumnType("boolean")
                        .HasColumnName("is_root_service");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name")
                        .UseCollation("ci_collation");

                    b.Property<Guid>("TenantId")
                        .HasColumnType("uuid")
                        .HasColumnName("tenant_id");

                    b.Property<string>("Url")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("url");

                    b.HasKey("Id")
                        .HasName("pk_service");

                    b.HasIndex("TenantId", "Name")
                        .IsUnique()
                        .HasDatabaseName("ix_service_tenant_id_name");

                    b.ToTable("service", (string)null);
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.ServiceHealthCache", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<int>("AggregateStatus")
                        .HasColumnType("integer")
                        .HasColumnName("aggregate_status");

                    b.Property<string>("Environment")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("environment")
                        .UseCollation("ci_collation");

                    b.Property<string>("Service")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("service")
                        .UseCollation("ci_collation");

                    b.Property<string>("Tenant")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("tenant")
                        .UseCollation("ci_collation");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("timestamp");

                    b.HasKey("Id")
                        .HasName("pk_service_health_cache");

                    b.HasIndex("Environment", "Tenant", "Service")
                        .IsUnique()
                        .HasDatabaseName("ix_service_health_cache_environment_tenant_service");

                    b.ToTable("service_health_cache", (string)null);
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.ServiceRelationship", b =>
                {
                    b.Property<Guid>("ServiceId")
                        .HasColumnType("uuid")
                        .HasColumnName("service_id");

                    b.Property<Guid>("ParentServiceId")
                        .HasColumnType("uuid")
                        .HasColumnName("parent_service_id");

                    b.HasKey("ServiceId", "ParentServiceId")
                        .HasName("pk_service_relationship");

                    b.HasIndex("ParentServiceId")
                        .HasDatabaseName("ix_service_relationship_parent_service_id");

                    b.ToTable("service_relationship", (string)null);
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.Tenant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("EnvironmentId")
                        .HasColumnType("uuid")
                        .HasColumnName("environment_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name")
                        .UseCollation("ci_collation");

                    b.HasKey("Id")
                        .HasName("pk_tenant");

                    b.HasIndex("EnvironmentId", "Name")
                        .IsUnique()
                        .HasDatabaseName("ix_tenant_environment_id_name");

                    b.ToTable("tenant", (string)null);
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("email")
                        .UseCollation("ci_collation");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("first_name")
                        .UseCollation("ci_collation");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("last_name")
                        .UseCollation("ci_collation");

                    b.HasKey("Id")
                        .HasName("pk_user");

                    b.HasIndex("Email")
                        .IsUnique()
                        .HasDatabaseName("ix_user_email");

                    b.ToTable("user", (string)null);
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.UserPermission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid?>("EnvironmentId")
                        .HasColumnType("uuid")
                        .HasColumnName("environment_id");

                    b.Property<int>("Permission")
                        .HasColumnType("integer")
                        .HasColumnName("permission");

                    b.Property<Guid?>("TenantId")
                        .HasColumnType("uuid")
                        .HasColumnName("tenant_id");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_user_permission");

                    b.HasIndex("EnvironmentId")
                        .HasDatabaseName("ix_user_permission_environment_id");

                    b.HasIndex("TenantId")
                        .HasDatabaseName("ix_user_permission_tenant_id");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_user_permission_user_id");

                    b.ToTable("user_permission", (string)null);
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.ApiKey", b =>
                {
                    b.HasOne("Cms.BatCave.Sonar.Data.Tenant", null)
                        .WithMany()
                        .HasForeignKey("TenantId")
                        .HasConstraintName("fk_api_key_tenant_tenant_id");
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.HealthCheck", b =>
                {
                    b.HasOne("Cms.BatCave.Sonar.Data.Service", null)
                        .WithMany()
                        .HasForeignKey("ServiceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_health_check_service_service_id");
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.HealthCheckCache", b =>
                {
                    b.HasOne("Cms.BatCave.Sonar.Data.ServiceHealthCache", null)
                        .WithMany()
                        .HasForeignKey("ServiceHealthId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_health_check_cache_service_health_cache_service_health_cach");
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.Service", b =>
                {
                    b.HasOne("Cms.BatCave.Sonar.Data.Tenant", null)
                        .WithMany()
                        .HasForeignKey("TenantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_service_tenant_tenant_id");
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.ServiceRelationship", b =>
                {
                    b.HasOne("Cms.BatCave.Sonar.Data.Service", null)
                        .WithMany()
                        .HasForeignKey("ParentServiceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_service_relationship_service_service_id1");

                    b.HasOne("Cms.BatCave.Sonar.Data.Service", null)
                        .WithMany()
                        .HasForeignKey("ServiceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_service_relationship_service_service_id");
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.Tenant", b =>
                {
                    b.HasOne("Cms.BatCave.Sonar.Data.Environment", null)
                        .WithMany()
                        .HasForeignKey("EnvironmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_tenant_environment_environment_id");
                });

            modelBuilder.Entity("Cms.BatCave.Sonar.Data.UserPermission", b =>
                {
                    b.HasOne("Cms.BatCave.Sonar.Data.Environment", null)
                        .WithMany()
                        .HasForeignKey("EnvironmentId")
                        .HasConstraintName("fk_user_permission_environment_environment_id");

                    b.HasOne("Cms.BatCave.Sonar.Data.Tenant", null)
                        .WithMany()
                        .HasForeignKey("TenantId")
                        .HasConstraintName("fk_user_permission_tenant_tenant_id");

                    b.HasOne("Cms.BatCave.Sonar.Data.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_user_permission_user_user_id");
                });
#pragma warning restore 612, 618
        }
    }
}
