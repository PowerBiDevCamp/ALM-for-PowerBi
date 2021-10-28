using DeploymentPipelineAutomation.Models;
using System;

namespace DeploymentPipelineAutomation {

  class Program {
  
    static void Main() {

      DemoStep01();
      DemoStep02();

    }

    static void DemoStep01() {
      PowerBiPipelineManager.DeleteAllWorkspaces();
      PowerBiPipelineManager.CreateDevWorkspace();
    }

    static void DemoStep02() {
      PowerBiPipelineManager.CreatePipelineWithWorkspaces();
    }

  }
}
