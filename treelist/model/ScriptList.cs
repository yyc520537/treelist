using DevExpress.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    public class TreeListNodeModel : INotifyPropertyChanged   
    {
        private int _id;
        private int? _parentId;
        private string _name;
        private string _fileTitle;
        private string _gitRevision;
        private string _gitDate;
        private int _group;
        private int _imageIndex;
        private string _fileid;
        private string _title;

        public int ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int? ParentID
        {
            get => _parentId;
            set => SetProperty(ref _parentId, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string FileTitle
        {
            get => _fileTitle;
            set => SetProperty(ref _fileTitle, value);
        }

        public string GitRevision
        {
            get => _gitRevision;
            set => SetProperty(ref _gitRevision, value);
        }

        public string GitDate
        {
            get => _gitDate;
            set => SetProperty(ref _gitDate, value);
        }

        public int Group
        {
            get => _group;
            set => SetProperty(ref _group, value);
        }

        public int ImageIndex
        {
            get => _imageIndex;
            set => SetProperty(ref _imageIndex, value);//在属性更改时触发 PropertyChanged 事件。
        }

        public string FileId
        {
            get => _fileid;
            set => SetProperty(ref _fileid, value);//在属性更改时触发 PropertyChanged 事件。
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);//在属性更改时触发 PropertyChanged 事件。
        }

        public event PropertyChangedEventHandler PropertyChanged; //处理属性更改事件的方法的委托

        protected virtual void OnPropertyChanged(string propertyName)//触发 PropertyChanged 事件（属性名）
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));//属性更改
        }

        //用于设置属性值，并在值更改时触发 PropertyChanged 事件。
        //使用泛型以处理不同类型的属性，并使用 CallerMemberName 特性来自动获取属性名称
        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))//比较旧值与新值
                return false;////shuxingzhiweigenggai1

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }


}
