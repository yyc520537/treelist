using DevExpress.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace treelist.model
{
    public class ScriptList
    {
        public string cfid { get; set; }
        public string checkedInBy { get; set; }
        public string gcid { get; set; }
        public string id { get; set; }
        public string nowRevisionNum { get; set; }
        public string otxStandards { get; set; }
        public string personnelRequirements { get; set; }
        public string qualifier { get; set; }
        public string scriptCondition { get; set; }
        public List<ScriptDTO> scriptDTOS { get; set; }
        public string scriptEvent { get; set; }
        public string scriptNum { get; set; }
        public string scriptObjective { get; set; }
        public string scriptProcess { get; set; }
        public string scriptType { get; set; }
        public string svnRevision { get; set; }
        public string svnScript { get; set; }
        public string tenantId { get; set; }
        public string title { get; set; }
        public string usageType { get; set; }
    }

    public class ScriptDTO
    {
        public string active { get; set; }
        public string id { get; set; }
        public string variantNo { get; set; }
        public string title { get; set; }
        public string fileTitle { get; set; }
        public List<ScGenericComponentDTO> scGenericComponentDTOS { get; set; }
        public List<string> profiles { get; set; }
        public string gitAuthor { get; set; }
        public string gitDate { get; set; }
        public string gitRevision { get; set; }
    }

    public class ScGenericComponentDTO
    {
        public string id { get; set; }
        public string displayId { get; set; }
        public string name { get; set; }
        public string active { get; set; }
        public string typeGroup { get; set; }
        public string type { get; set; }
        public string signalTypeDesignation { get; set; }
        public string releaseStatus { get; set; }
        public string location { get; set; }
        public string wdid { get; set; }
        public string ecu { get; set; }
        public string pins { get; set; }
        public object form { get; set; }
        public object to { get; set; }
        public object ecuAcromyms { get; set; }
        public object cableHarness { get; set; }
        public string isDeleted { get; set; }
        public string tenantId { get; set; }
        public string revision { get; set; }
        public string createdBy { get; set; }
        public DateTime createdTime { get; set; }
        public string updatedBy { get; set; }
        public DateTime updatedTime { get; set; }
    }

    public class TreeListNodeModel
    {
        public int ID { get; set; }
        public int? ParentID { get; set; }
        public string Name { get; set; }
        public string FileTitle { get; set; }
        public string GitRevision { get; set; }
        public string GitDate { get; set; }
        public int Group { get; set; }
        public int ImageIndex { get; set; }
    }


}
