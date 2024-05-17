using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace DBMSApp
{
    public partial class Form1 : Form
    {
        string connectionString = ConfigurationManager.AppSettings["connectionString"];
        SqlConnection connection;
        SqlDataAdapter adapterParent, adapterChild, adapterSecondParent;
        DataSet dataSet;
        BindingSource bindingSourceParent, bindingSourceChild, bindingSourceSecondParent;
        string parentTable = ConfigurationManager.AppSettings["parent"];
        string childTable = ConfigurationManager.AppSettings["child"];
        string secondParentTable = ConfigurationManager.AppSettings["secondParent"];

        SqlCommandBuilder cmdBuilder;

        string queryParent = "SELECT * FROM " + ConfigurationManager.AppSettings["parent"];
        string queryChild = "SELECT * FROM " + ConfigurationManager.AppSettings["child"];
        string querySecondParent = "SELECT * FROM " + ConfigurationManager.AppSettings["secondParent"];
        
        public Form1()
        {
            InitializeComponent();
            FillData();
        }

        string GetPrimaryKey(string tableName)
        {
            SqlCommand cmd = new SqlCommand("SELECT  COL_NAME(ic.OBJECT_ID,ic.column_id) AS ColumnName FROM    sys.indexes AS i INNER JOIN sys.index_columns AS ic ON  i.OBJECT_ID = ic.OBJECT_ID AND i.index_id = ic.index_id WHERE   i.is_primary_key = 1 AND OBJECT_NAME(ic.OBJECT_ID) = '" + tableName + "'", connection);
            return cmd.ExecuteScalar().ToString();
        }

        void FillData(){
            connection = new SqlConnection(connectionString);
            connection.Open();


            label1.Text = parentTable;

            label2.Text = childTable;

            label3.Text = secondParentTable;

            adapterParent = new SqlDataAdapter(queryParent, connection);
            adapterChild = new SqlDataAdapter(queryChild, connection);
            
            dataSet = new DataSet();
            if (secondParentTable == "")
            {
                adapterParent.Fill(dataSet, parentTable);
                adapterChild.Fill(dataSet, childTable);

                cmdBuilder = new SqlCommandBuilder(adapterChild);

                //get the primary key of the parent table from information schema
                string parentPK = GetPrimaryKey(parentTable);

                dataSet.Relations.Add("Parent_Child",
                    dataSet.Tables[parentTable].Columns[parentPK],
                    dataSet.Tables[childTable].Columns[parentPK]
                );

                bindingSourceParent = new BindingSource();
                bindingSourceParent.DataSource = dataSet.Tables[parentTable];

                bindingSourceChild = new BindingSource();
                bindingSourceChild.DataSource = bindingSourceParent;
                bindingSourceChild.DataMember = "Parent_Child";

                this.Manufacturers.DataSource = bindingSourceParent;
                this.Laptops.DataSource = bindingSourceChild;

                cmdBuilder.GetUpdateCommand();
                button1.Click += button1_Click;
            }else{
                adapterSecondParent = new SqlDataAdapter(querySecondParent, connection);
                adapterParent.Fill(dataSet, parentTable);
                adapterChild.Fill(dataSet, childTable);
                adapterSecondParent.Fill(dataSet, secondParentTable);

                cmdBuilder = new SqlCommandBuilder(adapterChild);

                //get the primary key of the parent table from information schema
                string parentPK = GetPrimaryKey(parentTable);
                string secondParentPK = GetPrimaryKey(secondParentTable);

                Manufacturers.DataSource = dataSet.Tables[parentTable];
                dataGridView1.DataSource = dataSet.Tables[secondParentTable];
                Laptops.DataSource = dataSet.Tables[childTable];

                Manufacturers.SelectionChanged += (s, e) =>
                {
                    if (Manufacturers.SelectedRows.Count > 0)
                    {
                        string selectedValue = Manufacturers.SelectedRows[0].Cells[parentPK].Value.ToString();
                        bindingSourceChild.Filter = "'"+parentPK + " = " + selectedValue+"'";
                    }
                };
                dataGridView1.SelectionChanged += (s, e) =>
                {
                    if (dataGridView1.SelectedRows.Count > 0)
                    {
                        string selectedValue = dataGridView1.SelectedRows[0].Cells[secondParentPK].Value.ToString();
                        bindingSourceChild.Filter = "'" + secondParentPK + " = " + selectedValue + "'";
                    }
                };

                cmdBuilder.GetUpdateCommand();
                button1.Click += button1_Click;
            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try { 
                adapterChild.Update(dataSet, childTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
