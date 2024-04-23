using DevExpress.XtraEditors.Filtering;
using DevExpress.XtraTreeList.Columns;
using DevExpress.XtraTreeList.Nodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace treelist
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        private int nodeId = 0; // 用于生成唯一的节点ID

        public Form1()
        {
            InitializeComponent();
            InitializeTreeList();
        }

        private void treeList1_FocusedNodeChanged(object sender, DevExpress.XtraTreeList.FocusedNodeChangedEventArgs e)
        {
            // 可以在这里添加逻辑来处理节点焦点改变事件
            if (e.Node != null)
            {
                Console.WriteLine("Selected Node: " + e.Node.GetValue("Name"));
            }
        }

        private void InitializeTreeList()
        {
            treeList1.Columns.Add(new TreeListColumn { Caption = "Name", FieldName = "Name", VisibleIndex = 0 });
            treeList1.Columns.Add(new TreeListColumn { Caption = "id", FieldName = "id", VisibleIndex = 1 });
            treeList1.KeyFieldName = "ID";
            treeList1.ParentFieldName = "ParentID";
        }

        //点击按钮，选择XML文件地址
        private void LoadXmlButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All files (*.*)|*.*";//XML files (*.xml)|*.xml|
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadXmlData(openFileDialog.FileName);//读取文件
            }
        }
        private void LoadXmlData(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(filePath);
                XmlToTree(xmlDoc.DocumentElement, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("XML文件读取错误: " + ex.Message);
            }
        }

        private void XmlToTree(XmlNode xmlNode, TreeListNode parentNode)
        {
            // 初始化节点名称为XML节点的标签名。
            string nodeName = xmlNode.Name;
            // 检查是否存在name属性，并且该属性是否有值。
            XmlAttribute nameAttribute = xmlNode.Attributes?["name"];
            if (nameAttribute != null && !string.IsNullOrEmpty(nameAttribute.Value))
            {
                nodeName = nameAttribute.Value; // 使用name属性的值作为节点名称。
            }
            string Id = "";
            XmlAttribute id = xmlNode.Attributes?["id"];
            if (id != null && !string.IsNullOrEmpty(id.Value))
            {
                Id = id.Value; // 使用name属性的值作为节点名称。
            }

            // 创建一个新的树节点，往treelist添加数据
            TreeListNode node = treeList1.AppendNode(new object[] { nodeName, Id }, parentNode);
            // 递归遍历子节点。
            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                // 只处理元素类型的节点。
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    XmlToTree(childNode, node);
                }
            }
        }
    }
}
