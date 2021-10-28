using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.PowerBI.Api.Models.Credentials;

namespace DeploymentPipelineAutomation.Models {

  class PowerBiPipelineManager {

    //static PowerBIClient pbiClient = TokenManager.GetPowerBiAppOnlyClient();
    static PowerBIClient pbiClient = TokenManager.GetPowerBiClient(PowerBiPermissionScopes.PipelineAdmin);

    #region "Configuration values"

    const string CAPACITY_ID = ""; // ADD GIUD for Premium Capacity

    const string PIPELINE_NAME = "Contoso Sales";
    const string PIPELINE_DESCRIPTION = "Sample pipeline for Power BI Dev Camp";

    const string WORKSPACE_NAME_DEV = "Contoso Sales Dev";
    const string WORKSPACE_NAME_TEST = "Contoso Sales Test";
    const string WORKSPACE_NAME_PROD = "Contoso Sales";
    const string WORKSPACE_ADMIN_USER = "austinp@powerbidevcamp.net";

    const string ROOT_FOLDER_PATH = @"C:\DevCamp\ALM-for-PowerBi\Artifacts\V1\";

    const string PBIX01_FILE_PATH = ROOT_FOLDER_PATH + "Customer Sales.pbix";
    const string PBIX01_IMPORT_NAME = "Customer Sales";

    const string PAGINATED01_FILE_PATH = ROOT_FOLDER_PATH + "Customer Sales Paginated.rdl";
    const string PAGINATED01_IMPORT_NAME = "Customer Sales Paginated";

    const string BIM_FILE_PATH = ROOT_FOLDER_PATH + "Product Sales Model.bim";
    const string BIM_DATASET_NAME = "Product Sales Model";

    const string PBIX02_FILE_PATH = ROOT_FOLDER_PATH + "Product Sales Model.pbix";
    const string PBIX02_IMPORT_NAME = "Product Sales Model";

    const string PBIX03_FILE_PATH = ROOT_FOLDER_PATH + "Product Sales Report.pbix";
    const string PBIX03_IMPORT_NAME = "Product Sales Report";

    const string PBIX04_FILE_PATH = ROOT_FOLDER_PATH + "Top 10 Products.pbix";
    const string PBIX04_IMPORT_NAME = "Top 10 Products";

    const string DATAFLOW_FILE_PATH = ROOT_FOLDER_PATH + "Product List.json";

    const string DATABASE_SERVER_DEV = "devcamp.database.windows.net";
    const string DATABASE_Name_DEV = "WingtipSalesDev";

    const string DATABASE_USER_NAME = "CptStudent";
    const string DATABASE_USER_PASSWORD = "pass@word1";

    #endregion

    #region "Private helper methods"

    private static Group GetWorkspace(string WorkspaceName) {
      var workspaces = pbiClient.Groups.GetGroups().Value;
      foreach (var workspace in workspaces) {
        if (workspace.Name.Equals(WorkspaceName))
          return workspace;
      }
      return null;
    }

    private static Group CreatWorkspace(string Name) {
      Console.WriteLine();
      Console.WriteLine("Creating workspace " + Name + "...");
      GroupCreationRequest request = new GroupCreationRequest(Name);
      Group workspace = pbiClient.Groups.CreateGroup(request);

      Guid CapacityId = new Guid(CAPACITY_ID);
      pbiClient.Groups.AssignToCapacity(workspace.Id, new AssignToCapacityRequest(CapacityId));

      // add user to workspace as Admin when creating workspace as service principal
      if (!string.IsNullOrEmpty(WORKSPACE_ADMIN_USER)) {
        pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser {
          EmailAddress = WORKSPACE_ADMIN_USER,
          GroupUserAccessRight = "Admin"
        });
      }
      return workspace;
    }

    private static void AddUserToWorkspace(Guid WorkspaceId, string UserEmail) {
      if (!string.IsNullOrEmpty(UserEmail)) {
        pbiClient.Groups.AddGroupUser(WorkspaceId, new GroupUser {
          EmailAddress = UserEmail,
          GroupUserAccessRight = "Admin"
        });
      }
    }

    private static void DeleteWorkspace(string WorkspaceName) {

      string filter = "name eq '" + WorkspaceName + "'";
      var workspaces = pbiClient.Groups.GetGroups(filter: filter).Value;

      if (workspaces.Count > 0) {
        var workspace = workspaces[0];
        Console.WriteLine("Deleting workspace - " + workspace.Name);
        pbiClient.Groups.DeleteGroup(workspace.Id);
      }
      else {
        Console.WriteLine("Workspace " + WorkspaceName + " does not exist");
      }
    }

    private static Dataset GetDataset(Guid WorkspaceId, string DatasetName) {
      var datasets = pbiClient.Datasets.GetDatasetsInGroup(WorkspaceId).Value;
      foreach (var dataset in datasets) {
        if (dataset.Name.Equals(DatasetName)) {
          return dataset;
        }
      }
      return null;
    }

    private static Report GetReport(Guid WorkspaceId, string ReporttName) {
      var reports = pbiClient.Reports.GetReportsInGroup(WorkspaceId).Value;
      foreach (var report in reports) {
        if (report.Name.Equals(ReporttName)) {
          return report;
        }
      }
      return null;
    }

    private static void DeleteReport(Guid WorkspaceId, string ReporttName) {
      var reports = pbiClient.Reports.GetReportsInGroup(WorkspaceId).Value;
      foreach (var report in reports) {
        if (report.Name.Equals(ReporttName)) {
          pbiClient.Reports.DeleteReportInGroup(WorkspaceId, report.Id);
          return;
        }
      }
    }

    private static void BindReportToDataset(Guid WorkspaceId, string DatasetName, string ReportName) {
      Console.WriteLine(" - Rebinding " + ReportName + " (report) to " + DatasetName + " (dataset)...");
      var dataset = GetDataset(WorkspaceId, DatasetName);
      var report = GetReport(WorkspaceId, ReportName);
      pbiClient.Reports.RebindReportInGroup(WorkspaceId, report.Id, new RebindReportRequest(dataset.Id));
    }

    private static void RefreshDatasetsInWorkspace(Guid WorkspaceId) {
      var datasets = pbiClient.Datasets.GetDatasetsInGroup(WorkspaceId).Value;
      foreach (var dataset in datasets) {
        if (dataset.IsRefreshable.HasValue ? dataset.IsRefreshable.Value : false) {
          pbiClient.Datasets.RefreshDatasetInGroup(WorkspaceId, dataset.Id);
        }
      }
    }

    private static Import ImportPBIX(Guid WorkspaceId, string PbixFilePath, string ImportName, bool SkipReport = false) {

      Console.WriteLine();
      Console.WriteLine("Importing " + ImportName + " from " + PbixFilePath);

      // open PBIX in file stream
      FileStream stream = new FileStream(PbixFilePath, FileMode.Open, FileAccess.Read);

      // post import to start import process
      var import = pbiClient.Imports.PostImportWithFileAsyncInGroup(WorkspaceId, stream, ImportName, ImportConflictHandlerMode.CreateOrOverwrite, SkipReport).Result;

      // poll to determine when import operation has complete
      do { import = pbiClient.Imports.GetImportInGroup(WorkspaceId, import.Id); }
      while (import.ImportState.Equals("Publishing"));

      // return Import object to caller
      return import;
    }

    public static Import ImportRDL(Guid WorkspaceId, string RdlFilePath, string ImportName) {

      Console.WriteLine();
      Console.WriteLine("Importing " + ImportName + " from " + RdlFilePath);

      FileStream fileStream = new FileStream(RdlFilePath, FileMode.Open, FileAccess.Read);
      string rdlImportName = ImportName + ".rdl";


      var import = pbiClient.Imports.PostImportWithFileInGroup(WorkspaceId, fileStream, rdlImportName, ImportConflictHandlerMode.Abort);

      // poll to determine when import operation has complete
      do { import = pbiClient.Imports.GetImportInGroup(WorkspaceId, import.Id); }
      while (import.ImportState.Equals("Publishing"));


      return import;


    }

    private static Import ImportDataflow(Guid WorkspaceId, string ModelJsonFilePath) {

      Console.WriteLine();
      Console.WriteLine("Importing dataflow from " + ModelJsonFilePath);

      // open PBIX in file stream
      FileStream stream = new FileStream(ModelJsonFilePath, FileMode.Open, FileAccess.Read);

      // importing dataflow requires import name to be 'model.json'
      string ImportName = "model.json";

      // post import to start import process
      var import = pbiClient.Imports.PostImportWithFileInGroup(WorkspaceId, stream, ImportName, ImportConflictHandlerMode.GenerateUniqueName);

      // poll to determine when import operation has complete
      do { import = pbiClient.Imports.GetImportInGroup(WorkspaceId, import.Id); }
      while (import.ImportState.Equals("Publishing"));

      var dataflows = pbiClient.Dataflows.GetDataflows(WorkspaceId).Value;

      var dataflow = dataflows[0];

      var datasources = pbiClient.Dataflows.GetDataflowDataSources(WorkspaceId, dataflow.ObjectId).Value;

      Console.WriteLine("Dataflow: " + dataflow.Name);
      Console.WriteLine("Object ID: " + dataflow.ObjectId);

      // find the target SQL datasource
      foreach (var datasource in datasources) {
        if (datasource.DatasourceType.ToLower() == "sql") {
          // get the datasourceId and the gatewayId
          var datasourceId = datasource.DatasourceId;
          var gatewayId = datasource.GatewayId;
          // Create UpdateDatasourceRequest to update Azure SQL datasource credentials
          UpdateDatasourceRequest req = new UpdateDatasourceRequest {
            CredentialDetails = new CredentialDetails(
              new BasicCredentials(DATABASE_USER_NAME, DATABASE_USER_PASSWORD),
              PrivacyLevel.None,
              EncryptedConnection.NotEncrypted)
          };
          // Execute Patch command to update Azure SQL datasource credentials
          pbiClient.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);
        }
      };

      pbiClient.Dataflows.RefreshDataflow(WorkspaceId, dataflow.ObjectId, new RefreshRequest { NotifyOption = "MailOnCompletion" });


      // return Import object to caller
      return import;
    }

    private static void PatchSqlDatasourceCredentials(Guid WorkspaceId, string DatasetId, string SqlUserName, string SqlUserPassword) {

      var datasources = (pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId)).Value;

      // find the target SQL datasource
      foreach (var datasource in datasources) {
        if (datasource.DatasourceType.ToLower() == "sql") {
          // get the datasourceId and the gatewayId
          var datasourceId = datasource.DatasourceId;
          var gatewayId = datasource.GatewayId;
          // Create UpdateDatasourceRequest to update Azure SQL datasource credentials
          UpdateDatasourceRequest req = new UpdateDatasourceRequest {
            CredentialDetails = new CredentialDetails(
              new BasicCredentials(SqlUserName, SqlUserPassword),
              PrivacyLevel.None,
              EncryptedConnection.NotEncrypted)
          };
          // Execute Patch command to update Azure SQL datasource credentials
          pbiClient.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);
        }
      };

    }

    public static Pipeline GetPipeline(string Name) {

      var pipelines = pbiClient.Pipelines.GetPipelinesAsAdmin().Value;
      foreach (var pipeline in pipelines) {
        if (pipeline.DisplayName.Equals(Name)) {
          return pipeline;
        }
      }
      return null;
    }

    public static void GetPipelineArtifacts(string Name) {

      Pipeline pipeline = GetPipeline(Name);

      Console.WriteLine("Artifacts for " + pipeline.DisplayName);
      var artifacts = pbiClient.Pipelines.GetPipelineStageArtifacts(pipeline.Id, 0);
      Console.WriteLine();

      Console.WriteLine("-- Datasets");
      foreach (var dataset in artifacts.Datasets) {
        Console.WriteLine("   * " + dataset.ArtifactDisplayName);
      }
      Console.WriteLine();
      Console.WriteLine("-- Reports");
      foreach (var report in artifacts.Reports) {
        Console.WriteLine("   * " + report.ArtifactDisplayName);
      }
      Console.WriteLine();


    }

    public static void GetPipelineOperations(string Name) {

      Pipeline pipeline = GetPipeline(Name);

      Console.WriteLine("Operations for " + pipeline.DisplayName);
      var operaions = pbiClient.Pipelines.GetPipelineOperations(pipeline.Id).Value;
      Console.WriteLine();

      foreach (var operation in operaions) {
        Console.WriteLine();
        Console.WriteLine(operation.Type);
        Console.WriteLine(operation.Id);
        Console.WriteLine(operation.LastUpdatedTime);
        Console.WriteLine(operation.SourceStageOrder);
        Console.WriteLine(operation.TargetStageOrder);
        Console.WriteLine(operation.ExecutionStartTime);
        Console.WriteLine(operation.ExecutionPlan);
      }

    }

    public static void DeployToPipelineStage(string Name, int Stage, bool FirstDeploy = false) {

      Console.Write("Deploying pipeline opetation from stage " + Stage + " to stage " + (Stage + 1));

      Pipeline pipeline = GetPipeline(Name);

      DeployAllRequest deployRequest = new DeployAllRequest {
        SourceStageOrder = Stage,
        Options = new DeploymentOptions {
          AllowOverwriteArtifact = true,
          AllowTakeOver = true,
          AllowCreateArtifact = true,
          AllowPurgeData = true
        }
      };

      if (FirstDeploy) {
        deployRequest.NewWorkspace = new PipelineNewWorkspaceRequest {
          Name = ((Stage == 0) ? WORKSPACE_NAME_TEST : WORKSPACE_NAME_PROD),
          CapacityId = new Guid(CAPACITY_ID)
        };
      }

      var deployResult = pbiClient.Pipelines.DeployAll(pipeline.Id, deployRequest);
      var deployOperationId = deployResult.Id;

      while (deployResult.Status.Equals("NotStarted") || deployResult.Status.Equals("Executing")) {
        Console.Write(".");
        deployResult = pbiClient.Pipelines.GetPipelineOperation(pipeline.Id, deployOperationId);
        System.Threading.Thread.Sleep(1000);
      }

    }

    public static void DeployPartialToPipelineTestStage(string Name) {

      Pipeline pipeline = GetPipeline(Name);

      SelectiveDeployRequest deployRequest = new SelectiveDeployRequest {
        SourceStageOrder = 0,
        Reports = new List<DeployArtifactRequest> {
          new DeployArtifactRequest{
          }
        },
        Options = new DeploymentOptions {
          AllowOverwriteArtifact = true,
          AllowTakeOver = true
        }
      };

      pbiClient.Pipelines.SelectiveDeploy(pipeline.Id, deployRequest);
    }

    public static void GetPipelines() {

      var pipelines = pbiClient.Pipelines.GetPipelinesAsAdmin().Value;
      foreach (var pipeline in pipelines) {
        Console.WriteLine(pipeline.DisplayName);
        var artifacts = pbiClient.Pipelines.GetPipelineStageArtifacts(pipeline.Id, 0);
        Console.WriteLine("== Datasets ==");
        foreach (var dataset in artifacts.Datasets) {
          Console.WriteLine("==  " + dataset.ArtifactDisplayName);
        }
        Console.WriteLine("== Reports ==");
        foreach (var report in artifacts.Reports) {
          Console.WriteLine("==  " + report.ArtifactDisplayName);
        }
        Console.WriteLine();
      }
    }


    #endregion

    public static void DeleteAllWorkspaces() {
      DeleteWorkspace(WORKSPACE_NAME_DEV);
      DeleteWorkspace(WORKSPACE_NAME_TEST);
      DeleteWorkspace(WORKSPACE_NAME_PROD);
    }

    public static void CreateDevWorkspace() {

      var workspace = CreatWorkspace(WORKSPACE_NAME_DEV);

      ImportCustomerSalesReport(workspace.Id);
      ImportCustomerSalesReportPaginated(workspace.Id);
      ImportProductSalesModel(workspace.Id);
      ImportProductSalesReport(workspace.Id);
      ImportTop10ProductReport(workspace.Id);
      ImportProductListDataflow(workspace.Id);

      Console.WriteLine();
      Console.WriteLine(WORKSPACE_NAME_DEV + " now fully populated with V1 content");
      Console.WriteLine();
    }

    public static void ImportCustomerSalesReport(Guid WorkspaceId) {

      ImportPBIX(WorkspaceId, PBIX01_FILE_PATH, PBIX01_IMPORT_NAME);
      Dataset dataset = GetDataset(WorkspaceId, PBIX01_IMPORT_NAME);

      Console.WriteLine(" - Updating dataset parameters...");
      UpdateMashupParametersRequest req =
        new UpdateMashupParametersRequest(new List<UpdateMashupParameterDetails>() {
          new UpdateMashupParameterDetails { Name = "DatabaseServer", NewValue = DATABASE_SERVER_DEV },
          new UpdateMashupParameterDetails { Name = "DatabaseName", NewValue = DATABASE_Name_DEV }
      });

      pbiClient.Datasets.UpdateParametersInGroup(WorkspaceId, dataset.Id, req);

      Console.WriteLine(" - Patching datasourcre credentials...");
      PatchSqlDatasourceCredentials(WorkspaceId, dataset.Id, DATABASE_USER_NAME, DATABASE_USER_PASSWORD);

      Console.WriteLine(" - Starting dataset refresh operation...");
      pbiClient.Datasets.RefreshDatasetInGroup(WorkspaceId, dataset.Id);

    }

    public static void ImportCustomerSalesReportPaginated(Guid WorkspaceId) {

      var import = ImportRDL(WorkspaceId, PAGINATED01_FILE_PATH, PAGINATED01_IMPORT_NAME);

      return;

      var report = pbiClient.Reports.GetReportInGroup(WorkspaceId, import.Reports[0].Id);

      var reportDatasources = pbiClient.Reports.GetDatasourcesInGroup(WorkspaceId, report.Id);
      Datasource reportDatasource = reportDatasources.Value[0];

      var updateRdlDatasourceDetailsList = new List<UpdateRdlDatasourceDetails>() {
        new UpdateRdlDatasourceDetails {
          DatasourceName = "SqlDatabase",
          ConnectionDetails = new RdlDatasourceConnectionDetails {
          Server = DATABASE_SERVER_DEV,
          Database = DATABASE_Name_DEV
          }
        }
      };

      var updateDatasourcesRequest = new UpdateRdlDatasourcesRequest(updateRdlDatasourceDetailsList);
      Console.WriteLine(" - Updating connection string for paginated report...");
      pbiClient.Reports.UpdateDatasourcesInGroup(WorkspaceId, report.Id, updateDatasourcesRequest);

      // get datasource again after database path has been updated
      reportDatasources = pbiClient.Reports.GetDatasourcesInGroup(WorkspaceId, report.Id);
      reportDatasource = reportDatasources.Value[0];

      // get dataset ID and gateway ID required to patch credentials
      var datasourceId = reportDatasource.DatasourceId;
      var gatewayId = reportDatasource.GatewayId;

      // Create UpdateDatasourceRequest to update Azure SQL datasource credentials
      UpdateDatasourceRequest req = new UpdateDatasourceRequest {
        CredentialDetails = new CredentialDetails(
          new BasicCredentials(DATABASE_USER_NAME, DATABASE_USER_PASSWORD),
          PrivacyLevel.None,
          EncryptedConnection.NotEncrypted)
      };
      // Execute Patch command to update Azure SQL datasource credentials
      Console.WriteLine(" - Patching database credentials...");
      pbiClient.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);

    }

    public static void ImportProductSalesModel(Guid WorkspaceId) {

      ImportPBIX(WorkspaceId, PBIX02_FILE_PATH, PBIX02_IMPORT_NAME, true);
      Dataset dataset = GetDataset(WorkspaceId, PBIX02_IMPORT_NAME);

      Console.WriteLine(" - Updating dataset parameters...");
      UpdateMashupParametersRequest req =
        new UpdateMashupParametersRequest(new List<UpdateMashupParameterDetails>() {
          new UpdateMashupParameterDetails { Name = "DatabaseServer", NewValue = DATABASE_SERVER_DEV },
          new UpdateMashupParameterDetails { Name = "DatabaseName", NewValue = DATABASE_Name_DEV }
      });

      pbiClient.Datasets.UpdateParametersInGroup(WorkspaceId, dataset.Id, req);

      Console.WriteLine(" - Patching datasourcre credentials...");
      PatchSqlDatasourceCredentials(WorkspaceId, dataset.Id, DATABASE_USER_NAME, DATABASE_USER_PASSWORD);

      Console.WriteLine(" - Starting dataset refresh operation...");
      pbiClient.Datasets.RefreshDatasetInGroup(WorkspaceId, dataset.Id);

      //DeleteReport(WorkspaceId, PBIX02_IMPORT_NAME);

    }

    public static void DeployBimForProductSalesModel(Guid WorkspaceId) {
      TomDatasetManager.DeployDatabase(BIM_DATASET_NAME, BIM_FILE_PATH);
    }

    public static void ImportProductSalesReport(Guid WorkspaceId) {
      ImportPBIX(WorkspaceId, PBIX03_FILE_PATH, PBIX03_IMPORT_NAME);
      BindReportToDataset(WorkspaceId, BIM_DATASET_NAME, PBIX03_IMPORT_NAME);
    }

    public static void ImportTop10ProductReport(Guid WorkspaceId) {
      ImportPBIX(WorkspaceId, PBIX04_FILE_PATH, PBIX04_IMPORT_NAME);
      BindReportToDataset(WorkspaceId, BIM_DATASET_NAME, PBIX04_IMPORT_NAME);
    }

    public static void ImportProductListDataflow(Guid WorkspaceId) {
      ImportDataflow(WorkspaceId, DATAFLOW_FILE_PATH);
    }

    public static void CreatePipelineWithWorkspaces() {

      var pipeline = pbiClient.Pipelines.CreatePipeline(
        new CreatePipelineRequest {
          DisplayName = PIPELINE_NAME,
          Description = PIPELINE_DESCRIPTION
        });

      var workspaceDev = GetWorkspace(WORKSPACE_NAME_DEV);

      pbiClient.Pipelines.AssignWorkspace(pipeline.Id, 0, new AssignWorkspaceRequest(workspaceDev.Id));
      pbiClient.Pipelines.UpdatePipelineUser(pipeline.Id, new PipelineUser {
        Identifier = WORKSPACE_ADMIN_USER,
        AccessRight = "Admin",
        PrincipalType = "User"
      });

      DeployToPipelineStage(PIPELINE_NAME, 0, true);
      Group workspaceTest = GetWorkspace(WORKSPACE_NAME_TEST);
      AddUserToWorkspace(workspaceTest.Id, WORKSPACE_ADMIN_USER);
      RefreshDatasetsInWorkspace(workspaceTest.Id);

      DeployToPipelineStage(PIPELINE_NAME, 1, true);
      Group workspaceProd = GetWorkspace(WORKSPACE_NAME_PROD);
      AddUserToWorkspace(workspaceProd.Id, WORKSPACE_ADMIN_USER);
      RefreshDatasetsInWorkspace(workspaceProd.Id);

    }

  }
}
