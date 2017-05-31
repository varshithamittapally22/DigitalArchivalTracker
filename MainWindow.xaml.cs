using System.Windows;
using System.Windows.Forms;
using System.IO;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data;
using System.Data.SQLite;
using System.Dynamic;
using Excel = Microsoft.Office.Interop.Excel;

namespace DigitalArchivalTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // AIP Report Module
        string selectedSheetName;
        string givenCustomSheetName;
        bool canExtractExcelData;
        bool isIDColPresent;
        private static string databaseName = "database.db";
        string tableName;
        bool hasTableLoaded = false;

        List<string> excelColNames = new List<string>();

        string excelFilePath = String.Empty;

        // Universal SQLiteConnection
        private static SQLiteConnection sqlConnection = new SQLiteConnection("Data Source='" + databaseName + "';Version=3;New=True;Compress=True;");

        private static SQLiteDataAdapter sqlDataAdapter;

        private static SQLiteCommandBuilder sqlCommandBuilder;

        // Fixity Parsing Module
        FixityParsing fixityParsing;


        public MainWindow()
        {
            InitializeComponent();
            fillExistingTableNamesCB();
        }

        /// <summary>
        /// ---------------------------------------------------------------------------------------------------------------------------------------------
        /// ----                                           ----------------------------                                                              ----
        /// ----                                           ----------------------------                                                              ----
        /// -----------------------------------------------AIP REPORT MODULE BEGINS-----------------------------------------------
        /// -----------------------------------------------AIP REPORT MODULE BEGINS-----------------------------------------------
        /// -----------------------------------------------AIP REPORT MODULE BEGINS-----------------------------------------------
        /// ----                                           ----------------------------                                                              ----
        /// ----                                           ----------------------------                                                              ----
        /// ---------------------------------------------------------------------------------------------------------------------------------------------
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 


        // -------------------------POINT 1: Selection on AIP Report Excel File
        private void aipReportSelectionBTN_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openExcelFile = new Microsoft.Win32.OpenFileDialog();
            openExcelFile.Filter = "Excel File (*.xlsx)|*.xlsx";
            if (openExcelFile.ShowDialog() == true)
            {
                aipReportPathTF.Text = openExcelFile.FileName;
                sheetNameCB.ItemsSource = lookForSheetNamesInExcel(aipReportPathTF.Text);
                excelFilePath = aipReportPathTF.Text;
                sheetNameCB.Text = "Select sheet";
            }

        }

        // -------------------------POINT 2: Finds the sheet names in selected Excel file and fill the sheet name combo box.
        private List<string> lookForSheetNamesInExcel(string excelFilePath)
        {
            List<string> listOfSheets = new List<string>();
            try
            {
                OleDbConnection conn = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + excelFilePath + ";Extended Properties=\"Excel 12.0 Xml;HDR=YES\"");
                conn.Open();
                DataTable dtSheet = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                foreach (DataRow row in dtSheet.Rows)
                {
                    if (row["TABLE_NAME"].ToString().Contains("$"))
                    {
                        string result = row["TABLE_NAME"].ToString().Replace("$", "");
                        listOfSheets.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error looking for sheet names in uploaded Excel. " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return listOfSheets;
        }

        // -------------------------POINT 3: Reads the sheet selected for the AIP Report Excel File.
        private void readAIPReportBTN_Click(object sender, RoutedEventArgs e)
        {
            //if (checkForAbnormalities(givenSheetNameTB.Text))
            //{
            //    System.Windows.MessageBox.Show("Table name cannot contain special characters, spaces or symbols.", "No special characters or spaces allowed!", MessageBoxButton.OK, MessageBoxImage.Warning);
            //}
            if (string.IsNullOrWhiteSpace(this.aipReportPathTF.Text) || sheetNameCB.SelectedIndex == -1 || string.IsNullOrWhiteSpace(givenSheetNameTB.Text))
            {
                System.Windows.MessageBox.Show("Please select an AIP Excel File, it's corresponding sheet from the list of sheet names and give it a custome name.", 
                    "Excel file not selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                selectedSheetName = sheetNameCB.SelectedItem.ToString();
                if (checkForAbnormalities(selectedSheetName))
                {
                    System.Windows.MessageBox.Show("Select a sheet that does not contain special characters, spaces or symbols.", "No special characters or spaces allowed!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    givenCustomSheetName = givenSheetNameTB.Text;
                    sqlConnection.Open();
                    DataTable dt = sqlConnection.GetSchema("Tables");
                    bool duplicateTable = false;
                    foreach (DataRow row in dt.Rows)
                    {
                        if (givenCustomSheetName == (string)row[2])
                        {
                            duplicateTable = true;
                        }
                    }

                    sqlConnection.Close();
                    if (duplicateTable)
                    {
                        System.Windows.MessageBox.Show("A table with the same custom sheet name exists. Try a different name.", "Duplicate table!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        // Setting universal tableName
                        tableName = givenCustomSheetName;
                        canExtractExcelData = true;
                        getExcelColumnNames();
                        isIDColPresent = false;
                        if (excelColNames.Contains("ID"))
                        {
                            isIDColPresent = true;
                        }
                        
                        if (excelColNames.Count > 0)
                        {
                            excelColNames.Clear();
                        }
                        if (canExtractExcelData)
                        {
                            if (isIDColPresent)
                            {
                                createTable(tableName, getExcelColumnNames());
                                //alternateInsertIntoDB();
                                List<ExpandoObject> excelData = convertAndReturnExcelDataForDB(getExcelColumnNames());
                                insertExcelDataIntoDB(excelData);
                                fillExistingTableNamesCB();
                            }
                            else
                            {
                                System.Windows.MessageBox.Show("ID column not present in Excel sheet. Please create an ID column and fill it with numbers.",
                                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Cannot create table. One or more column names in the Excel sheet have special characters, spaces or symbols."
                                , "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
        }

        private bool checkForAbnormalities(string str)
        {
            if (str.Contains(" ") || str.Contains("-") ||
                str.Contains("!") || str.Contains("@") ||
                str.Contains("_") || str.Contains(".") ||
                str.Contains(":") || str.Contains(";") ||
                str.Contains("'") || str.Contains("?") ||
                str.Contains("(") || str.Contains(")") ||
                str.Contains("{") || str.Contains("}") ||
                str.Contains("[") || str.Contains("]") ||
                str.Contains("|") || str.Contains("<") ||
                str.Contains(">") || str.Contains("%") ||
                str.Contains("^") || str.Contains("&"))
            {
                return true;
            }
            else { 
                return false;
            }
        }

        private void createTable(string tableName, List<string> excelColNames)
        {
            try
            {
                SQLiteConnection localConn = new SQLiteConnection("Data Source='" + databaseName + "';Version=3;New=True;Compress=True;");
                localConn.Open();
                SQLiteCommand sqlCommand = localConn.CreateCommand();
                
                string createString = "create table '" + tableName + "' (";
                int count = excelColNames.Count;
                foreach (string colName in excelColNames)
                {
                    if (count >= 1)
                    {
                        createString += colName;
                        if (colName == "ID")
                        {
                            createString += " int";
                        }
                        else
                        {
                            createString += " varchar(100)";
                        }
                        count--;
                        if (count >= 1)
                        {
                            createString += ", ";
                        }

                    }
                }
                createString += ", Primary Key(ID));";
                
                sqlCommand.CommandText = createString;
                sqlCommand.ExecuteNonQuery();
                sqlCommand.Dispose();
                localConn.Close();
                System.Windows.MessageBox.Show("Table '" + tableName + "' created!", "Task Successful", MessageBoxButton.OK ,MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<string> getExcelColumnNames()
        {
            try
            {
                var sheetName = sheetNameCB.SelectedItem.ToString();
                string query = "select * from [" + sheetName + "$]";
                OleDbConnection conn = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + this.aipReportPathTF.Text + ";Extended Properties=\"Excel 12.0 Xml;HDR=YES\"");
                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                DataTable dtExcel = new DataTable();
                adapter.Fill(dtExcel);
                int numOfCols = dtExcel.Columns.Count;
                foreach (DataColumn col in dtExcel.Columns)
                {
                    string colName = col.ColumnName;
                    if (checkForAbnormalities(colName))
                    {
                        canExtractExcelData = false;
                    }
                    excelColNames.Add(colName);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return excelColNames;
        }

        private List<ExpandoObject> convertAndReturnExcelDataForDB(List<string> excelColNames)
        {
            OleDbConnection conn;
            conn = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + this.aipReportPathTF.Text + ";Extended Properties=\"Excel 12.0 Xml;HDR=YES\"");
            conn.Open();
            OleDbCommand command = new OleDbCommand();
            command.Connection = conn;
            command.CommandText = "select * from [" + sheetNameCB.SelectedItem.ToString() + "$]";
            OleDbDataReader reader = command.ExecuteReader();

            List<ExpandoObject> dynamicAIPObjList = new List<ExpandoObject>();
            while (reader.Read())
            {
                dynamic expandoObj = new ExpandoObject();
                foreach (string colNameToPropName in excelColNames)
                {
                    string propValue = reader[colNameToPropName].ToString();
                    
                    AddProperty(expandoObj, colNameToPropName, propValue);
                }
                dynamicAIPObjList.Add(expandoObj);
            }

            reader.Close();
            conn.Close();
            
            return dynamicAIPObjList;
        }

        public static void AddProperty(ExpandoObject expando, string propertyName, string propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        private void insertExcelDataIntoDB(List<ExpandoObject> expandoList)
        {
            try
            {
                //sqlConnection.Open();
                SQLiteConnection localConn = new SQLiteConnection("Data Source='" + databaseName + "';Version=3;New=True;Compress=True;");
                localConn.Open();
                SQLiteCommand sqlCommand = localConn.CreateCommand();
                
                //Retreiving db col names
                string retrieveColNames = "select * from '" + tableName + "'";
                List<string> dbColNames = new List<string>();
                SQLiteDataAdapter sqlLocalDA = new SQLiteDataAdapter(retrieveColNames, localConn);
                DataTable dtDB = new DataTable();
                sqlLocalDA.Fill(dtDB);
                foreach (DataColumn col in dtDB.Columns)
                {
                    dbColNames.Add(col.ColumnName);
                }
                List<string> insertList = new List<string>();

                string testString = "";
                //Begin inserting expando objects
                foreach (ExpandoObject obj in expandoList)
                {
                    testString = "insert into '" + tableName + "' (";
                    int count = dbColNames.Count;
                    foreach (string colName in dbColNames)
                    {
                        if (count >= 1)
                        {
                            testString += colName;
                            count--;
                            if (count >= 1)
                            {
                                testString += ", ";
                            }

                        }
                    }
                    testString += ") values (";

                    int countProperties = ((IDictionary<string, object>)obj).Count;
                    foreach (var property in (IDictionary<string, object>)obj)
                    {
                        if (countProperties >= 1)
                        {
                            testString += "'" + property.Value + "'";
                            countProperties--;
                            if (countProperties >= 1)
                            {
                                testString += ",";
                            }
                        }

                    }
                    testString += ");";

                    insertList.Add(testString);
                }

                foreach (string insertQuery in insertList)
                {
                    try
                    {
                        sqlCommand.CommandText = insertQuery;
                        sqlCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                }

                putDataInDataGrid();
                existingTableNamesCB.Text = "Select table";
                localConn.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void putDataInDataGrid()
        {
            try
            {
                sqlConnection.Open();
                SQLiteCommand sqlCommand = sqlConnection.CreateCommand();
                sqlCommand.CommandText = "select * from '" + tableName + "'";
                sqlCommand.ExecuteNonQuery();
                sqlDataAdapter = new SQLiteDataAdapter(sqlCommand.CommandText, sqlConnection);
                sqlCommandBuilder = new SQLiteCommandBuilder(sqlDataAdapter);
                DataTable dt = new DataTable();
                sqlDataAdapter.Fill(dt);
                dgAIP.ItemsSource = dt.DefaultView;
                hasTableLoaded = true;
                sqlConnection.Close();
                loadColNameCB();
                currentTBL.Text = tableName;
                currentTBL.FontWeight = FontWeights.Bold;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void addColumnBTN_Click(object sender, RoutedEventArgs e)
        {
            if (hasTableLoaded)
            {
                if (!string.IsNullOrWhiteSpace(addColumnTB.Text))
                {
                    if (checkForAbnormalities(addColumnTB.Text))
                    {
                        System.Windows.MessageBox.Show("Column name cannot contain special characters, spaces or symbols.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        if (checkForDuplicateCol(addColumnTB.Text))
                        {
                            System.Windows.MessageBox.Show("Found duplicate");
                        }
                        else
                        {
                            try
                            {
                                sqlConnection.Open();
                                SQLiteCommand command = sqlConnection.CreateCommand();
                                string colToAdd = addColumnTB.Text;
                                command.CommandText = "alter table '" + tableName + "' add column " + colToAdd + " varchar(100);";
                                command.ExecuteNonQuery();
                                command = sqlConnection.CreateCommand();
                                command.CommandText = "select * from '" + tableName + "'";
                                sqlDataAdapter = new SQLiteDataAdapter(command.CommandText, sqlConnection);
                                sqlCommandBuilder = new SQLiteCommandBuilder(sqlDataAdapter);
                                DataTable sqlDT = new DataTable();
                                sqlDataAdapter.Fill(sqlDT);
                                dgAIP.ItemsSource = sqlDT.DefaultView;
                                loadColNameCB();
                                sqlConnection.Close();
                                System.Windows.MessageBox.Show("Column " + colToAdd + " added.", "Task Successful!", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            addColumnTB.Text = string.Empty;
                        }
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Column name cannot be empty!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please load a table from existing tables or upload a new AIP Excel file.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
        }

        private bool checkForDuplicateCol(string text)
        {
            try
            {
                DataTable dt = new DataTable();
                dt = ((DataView)dgAIP.ItemsSource).ToTable();
                foreach (DataColumn colName in dt.Columns)
                {
                    if (colName.ToString() == text)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
            return false;
        }

        private void deleteColBTN_Click(object sender, RoutedEventArgs e)
        {
            if (hasTableLoaded)
            {
                try
                {
                    if (columnNameDeleteCB.SelectedIndex != -1)
                    {
                        string colNameSelected = columnNameDeleteCB.SelectedItem.ToString();

                        List<string> colNames = new List<string>();
                        DataTable table = new DataTable();
                        table = ((DataView)dgAIP.ItemsSource).ToTable();

                        foreach (DataColumn col in table.Columns)
                        {
                            if (col.ToString() != colNameSelected)
                            {
                                colNames.Add(col.ToString());
                            }
                        }

                        sqlConnection.Open();

                        string oldTableName = tableName;
                        string newTableName = tableName + " 1";

                        string createString = "create table '" + newTableName + "' (";
                        int count = colNames.Count;
                        foreach (string colName in colNames)
                        {
                            if (count >= 1)
                            {
                                createString += colName;
                                if (colName == "ID")
                                {
                                    createString += " int";
                                }
                                else
                                {
                                    createString += " varchar(100)";
                                }
                                count--;
                                if (count >= 1)
                                {
                                    createString += ", ";
                                }

                            }
                        }
                        createString += ", Primary Key (ID));";

                        SQLiteCommand sqlCommand = sqlConnection.CreateCommand();
                        sqlCommand.CommandText = createString;
                        sqlCommand.ExecuteNonQuery();

                        //Initiating table data transfer
                        string dataTransfer = "insert into '" + newTableName + "' select ";
                        count = colNames.Count;
                        foreach (string colName in colNames)
                        {
                            if (count >= 1)
                            {
                                dataTransfer += colName;
                                count--;
                                if (count >= 1)
                                {
                                    dataTransfer += ", ";
                                }
                            }
                        }
                        dataTransfer += " from '" + oldTableName + "';";

                        sqlCommand = sqlConnection.CreateCommand();
                        sqlCommand.CommandText = dataTransfer;
                        sqlCommand.ExecuteNonQuery();

                        //Clearing data grid
                        dgAIP.ItemsSource = null;

                        //Deleting old table
                        sqlCommand = sqlConnection.CreateCommand();
                        sqlCommand.CommandText = "drop table '" + oldTableName + "'";
                        sqlCommand.ExecuteNonQuery();

                        //Renaming new table to old table to prevent confusing user when reusing the table
                        sqlCommand = sqlConnection.CreateCommand();
                        sqlCommand.CommandText = "alter table '" + newTableName + "' rename to '" + oldTableName + "'";
                        sqlCommand.ExecuteNonQuery();

                        tableName = oldTableName;

                        //Repopulating datagrid with new table
                        sqlCommand = sqlConnection.CreateCommand();
                        sqlCommand.CommandText = "select * from '" + tableName + "'";
                        sqlDataAdapter = new SQLiteDataAdapter(sqlCommand.CommandText, sqlConnection);
                        sqlCommandBuilder = new SQLiteCommandBuilder(sqlDataAdapter);
                        DataTable tempTable = new DataTable();
                        sqlDataAdapter.Fill(tempTable);
                        dgAIP.ItemsSource = tempTable.DefaultView;
                        loadColNameCB();
                        sqlConnection.Close();
                        columnNameDeleteCB.Text = "Select column";
                        System.Windows.MessageBox.Show("Successfully deleted: " + colNameSelected, "Task Successful!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please load a table or create one by importing an AIP Excel file.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void loadColNameCB()
        {
            if (hasTableLoaded)
            {
                DataTable table = new DataTable();
                table = ((DataView)dgAIP.ItemsSource).ToTable();
                List<string> colNames = new List<string>();
                foreach (DataColumn col in table.Columns)
                {
                    if (col.ToString() != "ID")
                    {
                        colNames.Add(col.ToString());
                    }
                }
                columnNameDeleteCB.ItemsSource = colNames;
            }
        }

        private void fillExistingTableNamesCB()
        {
            try
            {
                SQLiteConnection localConn = new SQLiteConnection("Data Source='" + databaseName + "';Version=3;New=True;Compress=True;");
                localConn.Open();
                List<string> tableList = new List<string>();
                DataTable dt = localConn.GetSchema("Tables");
                foreach (DataRow row in dt.Rows)
                {
                    tableList.Add((string)row[2]);
                }
                localConn.Close();
                if (tableList.Count > 0)
                {
                    existingTableNamesCB.ItemsSource = tableList;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void loadTableBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (existingTableNamesCB.SelectedIndex != -1)
                {
                    sqlConnection.Open();
                    SQLiteCommand command = sqlConnection.CreateCommand();
                    command.CommandText = "select * from '" + existingTableNamesCB.SelectedItem.ToString() + "';";
                    command.ExecuteNonQuery();

                    sqlDataAdapter = new SQLiteDataAdapter(command.CommandText, sqlConnection);
                    DataTable dt = new DataTable();
                    sqlDataAdapter.Fill(dt);
                    dgAIP.ItemsSource = dt.DefaultView;
                    sqlConnection.Close();
                    hasTableLoaded = true;

                    tableName = existingTableNamesCB.SelectedItem.ToString();
                    aipReportPathTF.Text = "AIP Excel Report Location";
                    sheetNameCB.Text = "Select sheet";
                    givenSheetNameTB.Text = string.Empty;
                    currentTBL.Text = tableName;
                    currentTBL.FontWeight = FontWeights.Bold;
                    System.Windows.MessageBox.Show("Successfully loaded table: " + existingTableNamesCB.SelectedItem.ToString());
                    loadColNameCB();
                }
                else
                {
                    System.Windows.MessageBox.Show("Please select a table to load.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void deleteTableBTN_Click(object sender, RoutedEventArgs e)
        {
            if (existingTableNamesCB.Items.Count > 0)
            {
                if (existingTableNamesCB.SelectedIndex != -1)
                {
                    try
                    {
                        tableName = existingTableNamesCB.SelectedItem.ToString();
                        if (dgAIP.ItemsSource != null)
                        {
                            dgAIP.ItemsSource = null;
                        }
                        sqlConnection = new SQLiteConnection("Data Source='" + databaseName + "';Version=3;New=True;Compress=True;");
                        sqlConnection.Open();
                        SQLiteCommand sqlCommand = sqlConnection.CreateCommand();
                        sqlCommand.CommandText = "drop table '" + tableName + "'";
                        sqlCommand.ExecuteNonQuery();
                        sqlConnection.Close();
                        existingTableNamesCB.Text = "Select table";
                        columnNameDeleteCB.Text = "Select column";
                        existingTableNamesCB.ItemsSource = null;
                        sheetNameCB.ItemsSource = null;
                        fillExistingTableNamesCB();
                        columnNameDeleteCB.ItemsSource = null;
                        hasTableLoaded = false;
                        currentTBL.Text = "";
                        aipReportPathTF.Text = "AIP Excel Report Location";
                        sheetNameCB.Text = "Select sheet";
                        givenSheetNameTB.Text = string.Empty;
                        
                        System.Windows.MessageBox.Show("'" + tableName + "' deleted!", "Task Successful!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Please select a table to delete.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Looks like your database doesn't have any tables. Select an AIP Excel sheet and create tables.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void submitBTN_Click(object sender, RoutedEventArgs e)
        {
            if (hasTableLoaded)
            {
                try
                {
                    // For situations when the user hasn't uploaded an Excel 
                    // file but just started the application and loaded a previously
                    // created table. This will initialize the necessary objects for CRUD operations.
                    if (sqlDataAdapter == null)
                    {
                        sqlConnection.Open();
                        SQLiteCommand command = sqlConnection.CreateCommand();
                        command.CommandText = "select * from '" + tableName + "'";
                        sqlDataAdapter = new SQLiteDataAdapter(command.CommandText, sqlConnection);
                        sqlCommandBuilder = new SQLiteCommandBuilder(sqlDataAdapter);
                        sqlDataAdapter.Update((dgAIP.ItemsSource as DataView).Table);
                        sqlConnection.Close();
                    }
                    else
                    {
                        sqlCommandBuilder = new SQLiteCommandBuilder(sqlDataAdapter);
                        sqlDataAdapter.Update((dgAIP.ItemsSource as DataView).Table);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Cannot save changes. Load a table from existing tables or upload a new AIP Excel file.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }
        
        private void exportCSVBTN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (hasTableLoaded)
                {
                    DataTable dt = new DataTable();
                    dt = ((DataView)dgAIP.ItemsSource).ToTable();
                    string fileName = string.Empty;
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "CSV file (*.csv)|*.csv";
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        fileName = sfd.FileName;
                        beginExportToCSV(dt, fileName);
                        System.Windows.MessageBox.Show("Successfully exported to " + fileName, "Export Successful!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Please load a table or create one and then try again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void beginExportToCSV(DataTable dt, string fileName)
        {
            try
            {
                StreamWriter sw = new StreamWriter(fileName, false);
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    sw.Write(dt.Columns[i]);
                    if (i < dt.Columns.Count - 1)
                    {
                        sw.Write(",");
                    }
                }
                //sw.WriteLine(sw.NewLine);
                sw.Write(sw.NewLine);
                foreach (DataRow dr in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (!Convert.IsDBNull(dr[i]))
                        {
                            string value = dr[i].ToString();
                            if (value.Contains(","))
                            {
                                value = String.Format("\"{0}\"", value);
                                sw.Write(value);
                            }
                            else
                            {
                                sw.Write(dr[i].ToString());
                            }
                        }
                        if (i < dt.Columns.Count - 1)
                        {
                            sw.Write(",");
                        }
                    }
                    sw.Write(sw.NewLine);
                }
                sw.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ---------------------------------------------------------------------------------------------------------------------------------------------
        /// ----                                           ----------------------------                                                              ----
        /// ----                                           ----------------------------                                                              ----
        /// -----------------------------------------------FIXITY PARSING MODULE -----------------------------------------------
        /// -----------------------------------------------FIXITY PARSING MODULE -----------------------------------------------
        /// -----------------------------------------------FIXITY PARSING MODULE -----------------------------------------------
        /// ----                                           ----------------------------                                                              ----
        /// ----                                           ----------------------------                                                              ----
        /// ---------------------------------------------------------------------------------------------------------------------------------------------
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void selectFixityReportBTN(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fBD = new FolderBrowserDialog();
            fBD.Description = "Select Fixity Report Folder";
            if (fBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fixityFolferPathTB.Text = fBD.SelectedPath;
                string[] files = Directory.GetFiles(fixityFolferPathTB.Text, "*.tsv");
                if (!(files.Length == 0))
                {
                    totalReportsLBL.Text = files.Length + " files";
                }
            }
        }

        private void parseFixityReportsBTN_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(fixityFolferPathTB.Text))
            {
                System.Windows.MessageBox.Show("Please select a folder where Fixity reports are stored and try again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            }
            else
            {
                string[] files = Directory.GetFiles(fixityFolferPathTB.Text, "*.tsv");
                if (!(files.Length == 0))
                {
                    fixityParsing = new FixityParsing(fixityFolferPathTB.Text, this);
                    fixityParsing.generateAllFixityReport();
                }
                else
                {
                    System.Windows.MessageBox.Show("Selected folder does not contain .tsv files.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void readFixityReportsBTN_Click(object sender, RoutedEventArgs e)
        {
            if (projectNamesCB.SelectedIndex != -1)
            {
                string projectNameSelected = projectNamesCB.SelectedItem.ToString();
                fixityParsing.printProjectResult(projectNameSelected);
            }
            else
            {
                if (projectNamesCB.Items.Count == 0)
                {
                    System.Windows.MessageBox.Show("Please load a Fixity Report folder and parse it to get the project names.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    System.Windows.MessageBox.Show("Please select a Fixity project from the drop down.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void clearDataBTN_Click(object sender, RoutedEventArgs e)
        {
            projectNameTB.Clear();
            totalFilesTB.Clear();
            confirmedFilesTB.Clear();
            movedOrRenamedTB.Clear();
            newFilesTB.Clear();
            changedFilesTB.Clear();
            projectNamesCB.SelectedIndex = -1;
            dgRemovedFilesList.ItemsSource = null;
            dgNewFilesList.ItemsSource = null;
            dgChangedFilesList.ItemsSource = null;
            dgConfirmedFilesList.ItemsSource = null;

            projectNamesCB.Text = "Select Project Name";
        }
    }
}
