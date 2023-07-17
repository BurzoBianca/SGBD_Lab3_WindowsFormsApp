using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp_LaAB3_SGBD
{
    public partial class Form1 : Form
    {
        private string connectionString = ConfigurationManager.AppSettings.Get("connectionString");
        private string name_ParentTable = ConfigurationManager.AppSettings.Get("parentTable");
        private string name_ChildTable = ConfigurationManager.AppSettings.Get("childTable");
        private string pk_ParentTable;
        private string pk_ChildTable;
        private string foreignKey;
        private string name_foreignKey;

        string currentId_Parent;
        string currentId_Child;

        DataGridViewRow currentRow_Child;

        DataSet ds;
        SqlDataAdapter parentTable;
        SqlDataAdapter childTable;


        public Form1()
        {
            InitializeComponent();
        }

        private void setKeys()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                String pkParent_sql;
                String pkChild_sql;
                String fk_sql;
                String fkName_sql;

                pkParent_sql = "DECLARE @pkParent varchar(100);" +
                    "select @pkParent = C.COLUMN_NAME  FROM  " +
                    "INFORMATION_SCHEMA.TABLE_CONSTRAINTS T " +
                    " JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE C " +
                    "ON C.CONSTRAINT_NAME = T.CONSTRAINT_NAME " +
                    "WHERE C.TABLE_NAME = '" + name_ParentTable +
                    "' and T.CONSTRAINT_TYPE = 'PRIMARY KEY'; " +
                    "SELECT @pkParent;";
                pkChild_sql = "DECLARE @pkChild varchar(100);" +
                    "select C.COLUMN_NAME FROM  " +
                    "INFORMATION_SCHEMA.TABLE_CONSTRAINTS T " +
                    " JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE C " +
                    "ON C.CONSTRAINT_NAME = T.CONSTRAINT_NAME " +
                    "WHERE C.TABLE_NAME = '" + name_ChildTable +
                    "' and T.CONSTRAINT_TYPE = 'PRIMARY KEY'; " +
                    "SELECT @pkChild;";
                fk_sql = "DECLARE @fkColumn varchar(100);" +
                    "SELECT @fkColumn = colpar.name " +
                    "FROM sys.foreign_keys foreignKey " +
                    "INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = foreignKey.object_id " +
                    "INNER JOIN sys.tables tpar ON foreignKey.parent_object_id = tpar.object_id " +
                    "INNER JOIN sys.columns colpar ON fkc.parent_object_id = colpar.object_id AND fkc.parent_column_id = colpar.column_id " +
                    "INNER JOIN sys.tables tref ON foreignKey.referenced_object_id = tref.object_id " +
                    "INNER JOIN sys.columns colref ON fkc.referenced_object_id = colref.object_id AND fkc.referenced_column_id = colref.column_id " +
                    "WHERE tpar.name = '" + name_ChildTable + "' AND tref.name = '" + name_ParentTable + "';" +
                    "SELECT @fkColumn;";
                fkName_sql = "select name, OBJECT_NAME(parent_object_id), OBJECT_NAME(referenced_object_id)" +
                               "from sys.foreign_keys where " +
                               "parent_object_id = OBJECT_ID('" + name_ChildTable + "'); ";

                try
                {
                    connection.Open();
                    MessageBox.Show("Yay");
                    SqlCommand cmd1 = new SqlCommand(pkParent_sql, connection);
                    pk_ParentTable = (string)cmd1.ExecuteScalar();
                    Console.WriteLine(pk_ParentTable);
                    SqlCommand cmd2 = new SqlCommand(pkChild_sql, connection);
                    pk_ChildTable = (string)cmd2.ExecuteScalar();
                    Console.WriteLine(pk_ChildTable);
                    SqlCommand cmd3 = new SqlCommand(fk_sql, connection);
                    foreignKey = (string)cmd3.ExecuteScalar();
                    Console.WriteLine(foreignKey);
                    SqlCommand cmd4 = new SqlCommand(fkName_sql, connection);
                    name_foreignKey = (string)cmd4.ExecuteScalar();
                    Console.WriteLine(name_foreignKey);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    connection.Close();
                }
                Console.ReadLine();
            }



        }

        private void connButton_Click(object sender, EventArgs e)
        {
            deleteButton.Enabled = false;
            updateButton.Enabled = false;
            setKeys();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    parentTable = new SqlDataAdapter("select * from " + name_ParentTable, connection);
                    childTable = new SqlDataAdapter("select * from " + name_ChildTable, connection);

                    SqlCommandBuilder cb = new SqlCommandBuilder(parentTable);
                    cb = new SqlCommandBuilder(childTable);

                    ds = new DataSet();
                    parentTable.Fill(ds, name_ParentTable);
                    childTable.Fill(ds, name_ChildTable);

                    DataRelation dr = new DataRelation(name_foreignKey, ds.Tables[name_ParentTable].Columns[pk_ParentTable], ds.Tables[name_ChildTable].Columns[foreignKey]);
                    ds.Relations.Add(dr);

                    BindingSource bindingSourcesParent = new BindingSource();
                    BindingSource bindingSourcesChild = new BindingSource();

                    bindingSourcesParent.DataSource = ds;
                    bindingSourcesParent.DataMember = name_ParentTable;
                    bindingSourcesChild.DataSource = bindingSourcesParent;
                    bindingSourcesChild.DataMember = pk_ParentTable;

                    dataGridViewParent.DataSource = bindingSourcesParent;
                    dataGridViewChild.DataSource = bindingSourcesChild;

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    connection.Close();
                }
                Console.ReadLine();
            }
        }

        private void dataGridViewParent_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            deleteButton.Enabled = false;
            updateButton.Enabled = false;
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dataGridViewParent.Rows[e.RowIndex];
                currentId_Parent = row.Cells[dataGridViewParent.Columns[pk_ParentTable].Index].Value.ToString();
                getDataChildTable(currentId_Parent);
            }
        }

        private void getDataChildTable(string id)
        {
            SqlConnection connection = new SqlConnection(connectionString);

            String sql;
            SqlCommand command;

            sql = "Select * FROM " + name_ChildTable + " WHERE " + foreignKey + " = @id";

            command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            try
            {
                connection.Open();

                childTable = new SqlDataAdapter(command);
                ds = new DataSet();
                childTable.Fill(ds, name_ChildTable);

                dataGridViewChild.DataSource = ds.Tables[name_ChildTable].DefaultView;

                dataGridViewChild.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
                dataGridViewChild.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                dataGridViewChild.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                connection.Close();
            }
            Console.ReadLine();

        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            String sql;
            SqlCommand command;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    sql = "delete " + name_ChildTable + " where " + pk_ChildTable + " = @id";
                    command = new SqlCommand(sql, connection);

                    command.Parameters.AddWithValue("@id", currentId_Child);
                    Console.WriteLine(currentId_Child);

                    connection.Open();
                    command.ExecuteNonQuery();
                    MessageBox.Show("Information Deleted");
                    getDataChildTable(currentId_Parent);

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    connection.Close();
                }
                Console.ReadLine();
            }
        }

        private void dataGridViewChild_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            deleteButton.Enabled = true;
            updateButton.Enabled = true;

            //making foreignKey colum read only
            dataGridViewChild.Columns[foreignKey].ReadOnly = true;

            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dataGridViewChild.Rows[e.RowIndex];
                currentRow_Child = row;
                currentId_Child = row.Cells[dataGridViewChild.Columns[pk_ChildTable].Index].Value.ToString();

                int nRowIndex = dataGridViewChild.Rows.Count - 1;
                if (e.RowIndex == nRowIndex)
                {
                    deleteButton.Enabled = false;
                    updateButton.Enabled = false;
                    insertButton.Enabled = true;
                }
                else
                {
                    deleteButton.Enabled = true;
                    updateButton.Enabled = true;
                    insertButton.Enabled = false;
                }
            }
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            try
            {

                childTable.UpdateCommand = new SqlCommandBuilder(childTable).GetUpdateCommand();
                childTable.Update(ds, name_ChildTable);
                MessageBox.Show("Updated");
                getDataChildTable(currentId_Parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            try
            {
                dataGridViewChild.Rows[currentRow_Child.Index].Cells[foreignKey].Value = currentId_Parent;
                childTable.UpdateCommand = new SqlCommandBuilder(childTable).GetUpdateCommand();
                childTable.Update(ds, name_ChildTable);
                MessageBox.Show("Inserted");
                getDataChildTable(currentId_Parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dataGridViewChild.Rows.RemoveAt(currentRow_Child.Index);
            }

        }
    }
}