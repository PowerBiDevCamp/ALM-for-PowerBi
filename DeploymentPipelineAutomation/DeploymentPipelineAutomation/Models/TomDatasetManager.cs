using System;
using AMO = Microsoft.AnalysisServices;
using Microsoft.AnalysisServices.Tabular;
using System.Configuration;
using System.IO;

namespace DeploymentPipelineAutomation.Models {
  class TomDatasetManager {

    private static string confidentialApplicationId = ConfigurationManager.AppSettings["confidential-application-id"];
    private static string confidentialApplicationSecret = ConfigurationManager.AppSettings["confidential-application-secret"];
    private static string tenantName = ConfigurationManager.AppSettings["tenant-name"];

    private static string workspaceConnectionUrl = ConfigurationManager.AppSettings["workspace-connection-url"];


    public static Server server = new Server();

    public static void ConnectToPowerBIAsServicePrincipal() {
      string workspaceConnection = workspaceConnectionUrl;
      string tenantId = tenantName;
      string appId = confidentialApplicationId;
      string appSecret = confidentialApplicationSecret;
      string connectStringServicePrincipal = $"DataSource={workspaceConnection};User ID=app:{appId}@{tenantId};Password={appSecret};";
      server.Connect(connectStringServicePrincipal);
    }

    public static void ConnectToPowerBIAsUser() {
      string workspaceConnection = workspaceConnectionUrl;
      string user = "tedp@powerbidevcamp.net";
      string accessToken = TokenManager.GetAccessToken(PowerBiPermissionScopes.ManageWorkspaceAssets);
      string connectStringUser = $"DataSource={workspaceConnection};Password={accessToken};";
      server.Connect(connectStringUser);
    }

    static TomDatasetManager() {
      ConnectToPowerBIAsUser();
    }

    public static void DisplayDatabases() {
      foreach (Database database in server.Databases) {
        Console.WriteLine(database.Name);
        Console.WriteLine(database.CompatibilityLevel);
        Console.WriteLine(database.CompatibilityMode);
        Console.WriteLine(database.EstimatedSize);
        Console.WriteLine(database.ID);
        Console.WriteLine();
      }
    }

    public static void GetDatabaseInfo(string Name) {

      Database database = server.Databases.GetByName(Name);

      Console.WriteLine("Name: " + database.Name);
      Console.WriteLine("ID: " + database.ID);
      Console.WriteLine("ModelType: " + database.ModelType);
      Console.WriteLine("CompatibilityLevel: " + database.CompatibilityLevel);
      Console.WriteLine("LastUpdated: " + database.LastUpdate);
      Console.WriteLine("EstimatedSize: " + database.EstimatedSize);
      Console.WriteLine("CompatibilityMode: " + database.CompatibilityMode);
      Console.WriteLine("LastProcessed: " + database.LastProcessed);
      Console.WriteLine("LastSchemaUpdate: " + database.LastSchemaUpdate);

    }

    public static void RefreshDatabaseModel(string Name) {
      Database database = server.Databases.GetByName(Name);
      database.Model.RequestRefresh(RefreshType.DataOnly);
      database.Model.SaveChanges();
    }

    public static Database CreateDatabase(string DatabaseName) {

      string newDatabaseName = server.Databases.GetNewName(DatabaseName);

      var database = new Database() {
        Name = newDatabaseName,
        ID = newDatabaseName,
        CompatibilityLevel = 1520,
        StorageEngineUsed = Microsoft.AnalysisServices.StorageEngineUsed.TabularMetadata,
        Model = new Model() {
          Name = DatabaseName + "-Model",
          Description = "A Demo Tabular data model with 1520 compatibility level."
        }
      };

      server.Databases.Add(database);
      database.Update(Microsoft.AnalysisServices.UpdateOptions.ExpandFull);

      return database;
    }

    public static Database CopyDatabase(string sourceDatabaseName, string DatabaseName) {

      Database sourceDatabase = server.Databases.GetByName(sourceDatabaseName);

      string newDatabaseName = server.Databases.GetNewName(DatabaseName);
      Database targetDatabase = CreateDatabase(newDatabaseName);
      sourceDatabase.Model.CopyTo(targetDatabase.Model);
      targetDatabase.Model.SaveChanges();

      targetDatabase.Model.RequestRefresh(RefreshType.Full);
      targetDatabase.Model.SaveChanges();

      return targetDatabase;
    }

    public static void DeployDatabase(string DatabaseName, string BimFilePath) {

      Database database = server.Databases.FindByName(DatabaseName);

      if (database == null) {
        Console.WriteLine("Creating dataset " + DatabaseName);
        database = CreateDatabase(DatabaseName);
      }

      var databaseId = database.ID;

      Console.WriteLine("Name: " + database.Name);
      Console.WriteLine("ID: " + database.ID);

      FileStream fileStream = new FileStream(BimFilePath, FileMode.Open, FileAccess.Read);
      StreamReader reader = new StreamReader(fileStream);
      string bimFileContent = reader.ReadToEnd();
      reader.Close();
      fileStream.Close();

      var databaseDefinition = AMO.JsonSerializer.DeserializeDatabase(bimFileContent, null, AMO.CompatibilityMode.PowerBI);
      databaseDefinition.Name = database.Name;
      databaseDefinition.ID = database.ID;

      Console.WriteLine("Deploying BIM file to dataset " + DatabaseName);

      var deploymentScript = JsonScripter.ScriptCreateOrReplace(databaseDefinition);

      server.Execute(deploymentScript);

      Console.WriteLine("Starting refresh for dataset " + DatabaseName);
      database.Model.RequestRefresh(RefreshType.Full);
      database.Model.SaveChanges();

      Console.WriteLine("All doen with BIM deployment");

    }

  }
}
