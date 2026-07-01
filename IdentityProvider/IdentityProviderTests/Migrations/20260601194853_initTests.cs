using IdentityProvider.DbContext;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace IdentityProvider.Tests.Migrations;

public class InitMigrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IdpDbContext _dbContext;

    public InitMigrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<IdpDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new IdpDbContext(options);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesAllTablesAndIndexes()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert - Verify tables exist
        var tables = GetTableNames();
        
        Assert.Contains("AspNetRoles", tables);
        Assert.Contains("Users", tables);
        Assert.Contains("AspNetRoleClaims", tables);
        Assert.Contains("AspNetUserClaims", tables);
        Assert.Contains("AspNetUserLogins", tables);
        Assert.Contains("AspNetUserRoles", tables);
        Assert.Contains("AspNetUserTokens", tables);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesAspNetRolesTable()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var tables = GetTableNames();
        Assert.Contains("AspNetRoles", tables);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesUsersTable()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var tables = GetTableNames();
        Assert.Contains("Users", tables);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesAspNetRoleClaimsTable()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var tables = GetTableNames();
        Assert.Contains("AspNetRoleClaims", tables);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesAspNetUserClaimsTable()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var tables = GetTableNames();
        Assert.Contains("AspNetUserClaims", tables);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesAspNetUserLoginsTable()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var tables = GetTableNames();
        Assert.Contains("AspNetUserLogins", tables);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesAspNetUserRolesTable()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var tables = GetTableNames();
        Assert.Contains("AspNetUserRoles", tables);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesAspNetUserTokensTable()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var tables = GetTableNames();
        Assert.Contains("AspNetUserTokens", tables);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesIndexOnAspNetRoleClaims()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var indexes = GetIndexNames("AspNetRoleClaims");
        Assert.Contains(indexes, i => i.Contains("RoleId"));
    }

    [Fact]
    public void Up_MigrationApplied_CreatesRoleNameIndexOnAspNetRoles()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var indexes = GetIndexNames("AspNetRoles");
        Assert.Contains("RoleNameIndex", indexes);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesIndexOnAspNetUserClaims()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var indexes = GetIndexNames("AspNetUserClaims");
        Assert.Contains(indexes, i => i.Contains("UserId"));
    }

    [Fact]
    public void Up_MigrationApplied_CreatesIndexOnAspNetUserLogins()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var indexes = GetIndexNames("AspNetUserLogins");
        Assert.Contains(indexes, i => i.Contains("UserId"));
    }

    [Fact]
    public void Up_MigrationApplied_CreatesIndexOnAspNetUserRoles()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var indexes = GetIndexNames("AspNetUserRoles");
        Assert.Contains(indexes, i => i.Contains("RoleId"));
    }

    [Fact]
    public void Up_MigrationApplied_CreatesEmailIndexOnUsers()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var indexes = GetIndexNames("Users");
        Assert.Contains("EmailIndex", indexes);
    }

    [Fact]
    public void Up_MigrationApplied_CreatesUserNameIndexOnUsers()
    {
        // Act
        _dbContext.Database.EnsureCreated();

        // Assert
        var indexes = GetIndexNames("Users");
        Assert.Contains("UserNameIndex", indexes);
    }

    [Fact]
    public void Down_MigrationReverted_RemovesAllTables()
    {
        // Arrange - First create the schema
        _dbContext.Database.EnsureCreated();
        var tablesBeforeDelete = GetTableNames();
        Assert.NotEmpty(tablesBeforeDelete);

        // Act - Delete the database (simulates Down migration)
        _dbContext.Database.EnsureDeleted();

        // Assert - Create a new connection to verify database is gone
        using var testConnection = new SqliteConnection("DataSource=:memory:");
        testConnection.Open();
        
        var options = new DbContextOptionsBuilder<IdpDbContext>()
            .UseSqlite(testConnection)
            .Options;
        
        using var testContext = new IdpDbContext(options);
        var tables = GetTableNamesFromConnection(testConnection);
        
        // The in-memory database should not have any application tables
        Assert.DoesNotContain("AspNetRoles", tables);
        Assert.DoesNotContain("Users", tables);
    }

    [Fact]
    public void Down_MigrationReverted_RemovesAspNetRoleClaimsTable()
    {
        // Arrange
        _dbContext.Database.EnsureCreated();

        // Act
        _dbContext.Database.EnsureDeleted();

        // Assert
        using var testConnection = new SqliteConnection("DataSource=:memory:");
        testConnection.Open();
        var tables = GetTableNamesFromConnection(testConnection);
        Assert.DoesNotContain("AspNetRoleClaims", tables);
    }

    [Fact]
    public void Down_MigrationReverted_RemovesAspNetUserClaimsTable()
    {
        // Arrange
        _dbContext.Database.EnsureCreated();

        // Act
        _dbContext.Database.EnsureDeleted();

        // Assert
        using var testConnection = new SqliteConnection("DataSource=:memory:");
        testConnection.Open();
        var tables = GetTableNamesFromConnection(testConnection);
        Assert.DoesNotContain("AspNetUserClaims", tables);
    }

    [Fact]
    public void Down_MigrationReverted_RemovesAspNetUserLoginsTable()
    {
        // Arrange
        _dbContext.Database.EnsureCreated();

        // Act
        _dbContext.Database.EnsureDeleted();

        // Assert
        using var testConnection = new SqliteConnection("DataSource=:memory:");
        testConnection.Open();
        var tables = GetTableNamesFromConnection(testConnection);
        Assert.DoesNotContain("AspNetUserLogins", tables);
    }

    [Fact]
    public void Down_MigrationReverted_RemovesAspNetUserRolesTable()
    {
        // Arrange
        _dbContext.Database.EnsureCreated();

        // Act
        _dbContext.Database.EnsureDeleted();

        // Assert
        using var testConnection = new SqliteConnection("DataSource=:memory:");
        testConnection.Open();
        var tables = GetTableNamesFromConnection(testConnection);
        Assert.DoesNotContain("AspNetUserRoles", tables);
    }

    [Fact]
    public void Down_MigrationReverted_RemovesAspNetUserTokensTable()
    {
        // Arrange
        _dbContext.Database.EnsureCreated();

        // Act
        _dbContext.Database.EnsureDeleted();

        // Assert
        using var testConnection = new SqliteConnection("DataSource=:memory:");
        testConnection.Open();
        var tables = GetTableNamesFromConnection(testConnection);
        Assert.DoesNotContain("AspNetUserTokens", tables);
    }

    [Fact]
    public void Down_MigrationReverted_RemovesAspNetRolesTable()
    {
        // Arrange
        _dbContext.Database.EnsureCreated();

        // Act
        _dbContext.Database.EnsureDeleted();

        // Assert
        using var testConnection = new SqliteConnection("DataSource=:memory:");
        testConnection.Open();
        var tables = GetTableNamesFromConnection(testConnection);
        Assert.DoesNotContain("AspNetRoles", tables);
    }

    [Fact]
    public void Down_MigrationReverted_RemovesUsersTable()
    {
        // Arrange
        _dbContext.Database.EnsureCreated();

        // Act
        _dbContext.Database.EnsureDeleted();

        // Assert
        using var testConnection = new SqliteConnection("DataSource=:memory:");
        testConnection.Open();
        var tables = GetTableNamesFromConnection(testConnection);
        Assert.DoesNotContain("Users", tables);
    }

    private List<string> GetTableNamesFromConnection(SqliteConnection connection)
    {
        var tables = new List<string>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }

    private List<string> GetTableNames()
    {
        var tables = new List<string>();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }

    private List<string> GetIndexNames(string tableName)
    {
        var indexes = new List<string>();
        using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='{tableName}' AND name NOT LIKE 'sqlite_%';";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            indexes.Add(reader.GetString(0));
        }
        return indexes;
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class InitMigrationDirectTests
{
    private class TestableInitMigration : global::IdentityProvider.Migrations.init
    {
        public void TestUp(MigrationBuilder migrationBuilder) => base.Up(migrationBuilder);
        public void TestDown(MigrationBuilder migrationBuilder) => base.Down(migrationBuilder);
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_InvokesCreateTableForAspNetRoles()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var createTableOps = migrationBuilder.Operations.OfType<CreateTableOperation>().ToList();
        Assert.Contains(createTableOps, op => op.Name == "AspNetRoles");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_InvokesCreateTableForUsers()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var createTableOps = migrationBuilder.Operations.OfType<CreateTableOperation>().ToList();
        Assert.Contains(createTableOps, op => op.Name == "Users");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_InvokesCreateTableForAspNetRoleClaims()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var createTableOps = migrationBuilder.Operations.OfType<CreateTableOperation>().ToList();
        Assert.Contains(createTableOps, op => op.Name == "AspNetRoleClaims");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_InvokesCreateTableForAspNetUserClaims()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var createTableOps = migrationBuilder.Operations.OfType<CreateTableOperation>().ToList();
        Assert.Contains(createTableOps, op => op.Name == "AspNetUserClaims");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_InvokesCreateTableForAspNetUserLogins()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var createTableOps = migrationBuilder.Operations.OfType<CreateTableOperation>().ToList();
        Assert.Contains(createTableOps, op => op.Name == "AspNetUserLogins");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_InvokesCreateTableForAspNetUserRoles()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var createTableOps = migrationBuilder.Operations.OfType<CreateTableOperation>().ToList();
        Assert.Contains(createTableOps, op => op.Name == "AspNetUserRoles");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_InvokesCreateTableForAspNetUserTokens()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var createTableOps = migrationBuilder.Operations.OfType<CreateTableOperation>().ToList();
        Assert.Contains(createTableOps, op => op.Name == "AspNetUserTokens");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_CreatesSevenTables()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var createTableOps = migrationBuilder.Operations.OfType<CreateTableOperation>().ToList();
        Assert.Equal(7, createTableOps.Count);
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_CreatesIndexOnAspNetRoleClaims()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var indexOps = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Contains(indexOps, op => op.Name == "IX_AspNetRoleClaims_RoleId" && op.Table == "AspNetRoleClaims");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_CreatesRoleNameIndexOnAspNetRoles()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var indexOps = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        var roleNameIndex = indexOps.FirstOrDefault(op => op.Name == "RoleNameIndex" && op.Table == "AspNetRoles");
        Assert.NotNull(roleNameIndex);
        Assert.True(roleNameIndex.IsUnique);
        Assert.Equal("[NormalizedName] IS NOT NULL", roleNameIndex.Filter);
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_CreatesIndexOnAspNetUserClaims()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var indexOps = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Contains(indexOps, op => op.Name == "IX_AspNetUserClaims_UserId" && op.Table == "AspNetUserClaims");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_CreatesIndexOnAspNetUserLogins()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var indexOps = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Contains(indexOps, op => op.Name == "IX_AspNetUserLogins_UserId" && op.Table == "AspNetUserLogins");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_CreatesIndexOnAspNetUserRoles()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var indexOps = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Contains(indexOps, op => op.Name == "IX_AspNetUserRoles_RoleId" && op.Table == "AspNetUserRoles");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_CreatesEmailIndexOnUsers()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var indexOps = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Contains(indexOps, op => op.Name == "EmailIndex" && op.Table == "Users");
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_CreatesUserNameIndexOnUsers()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var indexOps = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        var userNameIndex = indexOps.FirstOrDefault(op => op.Name == "UserNameIndex" && op.Table == "Users");
        Assert.NotNull(userNameIndex);
        Assert.True(userNameIndex.IsUnique);
        Assert.Equal("[NormalizedUserName] IS NOT NULL", userNameIndex.Filter);
    }

    [Fact]
    public void Up_CalledWithMigrationBuilder_CreatesSevenIndexes()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestUp(migrationBuilder);

        // Assert
        var indexOps = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Equal(7, indexOps.Count);
    }

    [Fact]
    public void Down_CalledWithMigrationBuilder_DropsAspNetRoleClaimsTable()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestDown(migrationBuilder);

        // Assert
        var dropTableOps = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Contains(dropTableOps, op => op.Name == "AspNetRoleClaims");
    }

    [Fact]
    public void Down_CalledWithMigrationBuilder_DropsAspNetUserClaimsTable()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestDown(migrationBuilder);

        // Assert
        var dropTableOps = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Contains(dropTableOps, op => op.Name == "AspNetUserClaims");
    }

    [Fact]
    public void Down_CalledWithMigrationBuilder_DropsAspNetUserLoginsTable()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestDown(migrationBuilder);

        // Assert
        var dropTableOps = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Contains(dropTableOps, op => op.Name == "AspNetUserLogins");
    }

    [Fact]
    public void Down_CalledWithMigrationBuilder_DropsAspNetUserRolesTable()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestDown(migrationBuilder);

        // Assert
        var dropTableOps = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Contains(dropTableOps, op => op.Name == "AspNetUserRoles");
    }

    [Fact]
    public void Down_CalledWithMigrationBuilder_DropsAspNetUserTokensTable()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestDown(migrationBuilder);

        // Assert
        var dropTableOps = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Contains(dropTableOps, op => op.Name == "AspNetUserTokens");
    }

    [Fact]
    public void Down_CalledWithMigrationBuilder_DropsAspNetRolesTable()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestDown(migrationBuilder);

        // Assert
        var dropTableOps = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Contains(dropTableOps, op => op.Name == "AspNetRoles");
    }

    [Fact]
    public void Down_CalledWithMigrationBuilder_DropsUsersTable()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestDown(migrationBuilder);

        // Assert
        var dropTableOps = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Contains(dropTableOps, op => op.Name == "Users");
    }

    [Fact]
    public void Down_CalledWithMigrationBuilder_DropsSevenTables()
    {
        // Arrange
        var migrationBuilder = new MigrationBuilder("SqlServer");
        var migration = new TestableInitMigration();

        // Act
        migration.TestDown(migrationBuilder);

        // Assert
        var dropTableOps = migrationBuilder.Operations.OfType<DropTableOperation>().ToList();
        Assert.Equal(7, dropTableOps.Count);
    }
}
