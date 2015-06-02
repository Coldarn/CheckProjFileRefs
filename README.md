# CheckProjFileRefs
CheckProjFileRefs is a minimal, stand-alone, command line utility which scans Visual Studio project files for references that differ from the local file system. This is similar to the "Show All Files" feature in Solution Explorer, but can handle multiple complex project files in a single automated run. Additionally, it also identifies duplicate file references that are all too easy to acumulate during merges.

The tool doesn't require Visual Studio to be present on the system to function, so is suitable for use on build servers or other headless environments.

## Example Output
```
D:\TFS_Data\Projects\Web8\Foo.Web\Foo.Web.csproj

  Files not in the project:
    D:\TFS_Data\Projects\Web8\Foo.Web\App_GlobalResources\Areas\Document\Document_LevelsController.Designer.cs
    D:\TFS_Data\Projects\Web8\Foo.Web\App_GlobalResources\Areas\NUIX\NUIXAdministration_NUIXController.resx
    D:\TFS_Data\Projects\Web8\Foo.Web\App_GlobalResources\JavaScript\Administration\Annotation\Javascript_Annotation_Form1.designer.cs
    D:\TFS_Data\Projects\Web8\Foo.Web\App_GlobalResources\JavaScript\Analysis\predict\prediction\JavaScript_CaseSetup_Predict_Prediction.fr.designer.cs
    D:\TFS_Data\Projects\Web8\Foo.Web\App_GlobalResources\JavaScript\Case\JavaScript_PeopleOrg.Designer.cs
    D:\TFS_Data\Projects\Web8\Foo.Web\App_GlobalResources\JavaScript\DocumentViewer\JavaScript_DocumentViewer.fr.resx
    D:\TFS_Data\Projects\Web8\Foo.Web\App_GlobalResources\JavaScript\Group\DocumentSecurity\BindersOverrides\JavaScript_Group_DocumentSecurity_BinderOverrides.designer.cs
    D:\TFS_Data\Projects\Web8\Foo.Web\Areas\Group\Models\GroupSelectorModel.cs
    D:\TFS_Data\Projects\Web8\Foo.Web\Areas\TroubleshootingAdministration\Models\ErrorModel.cs
    D:\TFS_Data\Projects\Web8\Foo.Web\Areas\UserAdmin\Models\ImportUsersResult.cs
    D:\TFS_Data\Projects\Web8\Foo.Web\config\WCFClientEndPoints.config

  Directories not in the project:
    D:\TFS_Data\Projects\Web8\Foo.Web\App_GlobalResources\Areas\DPM
    D:\TFS_Data\Projects\Web8\Foo.Web\bin
    D:\TFS_Data\Projects\Web8\Foo.Web\obj
    D:\TFS_Data\Projects\Web8\Foo.Web\Security\ClaimsAuthorization

  References not in the file system:
    D:\TFS_Data\Projects\External\dtSearch\x64\dtengine64.dll
    D:\TFS_Data\Projects\Certificates\FTIConsulting.PublicKey.snk
    D:\TFS_Data\Projects\Common8\Unity.Data.config
    D:\TFS_Data\Projects\Common8\Unity.Domain.config
    D:\TFS_Data\Projects\Common8\Unity.Common.config
    D:\TFS_Data\Projects\Common8\Unity.ProcessFramework.JobSubmitter.config

  References in the project more than once:
    D:\TFS_Data\Projects\Web8\Foo.Web\Areas\Fields\Models\ThresholdDisplaySettings.cs
    D:\TFS_Data\Projects\Web8\Foo.Web\Areas\Fields\Models\IssueDrillDetail.cs
```
