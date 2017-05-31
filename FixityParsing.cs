using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace DigitalArchivalTracker
{
    class FixityParsing
    {
        string fixityFolderPath;
        static Dictionary<string, Dictionary<string, List<string>>> fixityFilesCollection;
        MainWindow mainWindow;
        string[] fixityFileNames;
        List<string> projectNames = new List<string>();
        bool hasParsed = false;

        List<FixityData> fixityDataList = new List<FixityData>();

        public FixityParsing(string fixityFolder, MainWindow mainWindowObj)
        {
            fixityFolderPath = fixityFolder;
            mainWindow = mainWindowObj;
        }

        public void generateAllFixityReport()
        {
            fixityFileNames = Directory.GetFiles(fixityFolderPath);

            fixityFilesCollection = new Dictionary<string, Dictionary<string, List<string>>>();
            ASCIIEncoding code = new ASCIIEncoding();

            foreach (string fileName in fixityFileNames)
            {
                FixityData fixityDataObj = new FixityData();

                Dictionary<string, List<string>> fixityFileData = new Dictionary<string, List<string>>();
                List<string> changedFiles = new List<string>();
                List<string> confirmedFiles = new List<string>();
                List<string> removedFiles = new List<string>();
                List<string> newFiles = new List<string>();
                List<string> movedFiles = new List<string>();

                try
                {
                    using (StreamReader sr = new StreamReader(Path.GetFullPath(fileName), code, true, 5000000))
                    {
                        String line;
                        while (!(sr.EndOfStream))
                        {
                            line = sr.ReadLine();

                            if (line.StartsWith("Project name"))
                            {
                                string[] lineKey = line.Split(' ');
                                List<string> lineKeyValue = new List<string>();
                                lineKeyValue.Add(lineKey[5]);
                                fixityFileData.Add(lineKey[0] + " " + lineKey[1], lineKeyValue);
                                foreach (string projectName in lineKeyValue)
                                {
                                    fixityDataObj.projectName = projectName;
                                }
                            }

                            if (line.StartsWith("Total Files"))
                            {
                                string[] lineKey = line.Split(' ');
                                List<string> lineKeyValue = new List<string>();
                                lineKeyValue.Add(lineKey[2]);
                                fixityFileData.Add(lineKey[0] + " " + lineKey[1], lineKeyValue);
                                foreach (string totFilesNumber in lineKeyValue)
                                {
                                    fixityDataObj.totalFiles = totFilesNumber;
                                }
                            }
                            else if (line.StartsWith("Confirmed Files"))
                            {
                                string[] lineKey = line.Split(' ');
                                List<string> lineKeyValue = new List<string>();
                                lineKeyValue.Add(lineKey[2]);
                                fixityFileData.Add(lineKey[0] + " " + lineKey[1], lineKeyValue);
                                foreach (string conFilesNumber in lineKeyValue)
                                {
                                    fixityDataObj.confirmedFiles = conFilesNumber;
                                }
                            }
                            else if (line.StartsWith("Moved or Renamed Files"))
                            {
                                string[] lineKey = line.Split(' ');
                                List<string> lineKeyValue = new List<string>();
                                lineKeyValue.Add(lineKey[5]);
                                fixityFileData.Add(lineKey[0] + " " + lineKey[1] + " " + lineKey[2] + " " + lineKey[3], lineKeyValue);
                                foreach (string movedFileNumber in lineKeyValue)
                                {
                                    fixityDataObj.movedOrRenamedFiles = movedFileNumber;
                                }
                            }
                            else if (line.StartsWith("New Files"))
                            {
                                string[] lineKey = line.Split(' ');
                                List<string> lineKeyValue = new List<string>();
                                lineKeyValue.Add(lineKey[4]);
                                fixityFileData.Add(lineKey[0] + " " + lineKey[1], lineKeyValue);
                                foreach (string newFileNumber in lineKeyValue)
                                {
                                    fixityDataObj.newFiles = newFileNumber;
                                }
                            }
                            else if (line.StartsWith("Changed Files"))
                            {
                                string[] lineKey = line.Split(' ');
                                List<string> lineKeyValue = new List<string>();
                                lineKeyValue.Add(lineKey[4]);
                                fixityFileData.Add(lineKey[0] + " " + lineKey[1], lineKeyValue);
                                foreach (string changedFilesNumber in lineKeyValue)
                                {
                                    fixityDataObj.changedFiles = changedFilesNumber;
                                }
                            }
                            else if (line.StartsWith("Removed Files"))
                            {
                                string pattern = @"\s";
                                string[] lineKey = Regex.Split(line, pattern);
                                List<string> lineKeyValue = new List<string>();

                                if (lineKey.Length == 5)
                                {
                                    lineKeyValue.Add(lineKey[4]);
                                    fixityFileData.Add(lineKey[0] + " " + lineKey[1], lineKeyValue);
                                }
                                else
                                {
                                    pattern = @"\t";
                                    lineKey = Regex.Split(line, pattern);
                                    removedFiles.Add(lineKey[1]);
                                    if (fixityFileData.ContainsKey(lineKey[0]))
                                    {
                                        fixityFileData[lineKey[0]] = removedFiles;
                                    }
                                    else
                                    {
                                        fixityFileData.Add(lineKey[0], removedFiles);
                                    }
                                }

                                //Adding removed files info to Fixity Data class obj.
                                fixityDataObj.AddDataToRemovedFilesList(removedFiles);
                            }
                            else if (line.StartsWith("Changed File:"))
                            {
                                string pattern = @"\t";
                                string[] lineKey = Regex.Split(line, pattern);
                                changedFiles.Add(lineKey[1]);
                                if (fixityFileData.ContainsKey(lineKey[0]))
                                {
                                    fixityFileData[lineKey[0]] = changedFiles;
                                }
                                else
                                {
                                    fixityFileData.Add(lineKey[0], changedFiles);
                                }

                                fixityDataObj.AddDataToChangedFilesList(changedFiles);
                            }
                            else if (line.StartsWith("New File:"))
                            {
                                string pattern = @"\t";
                                string[] lineKey = Regex.Split(line, pattern);
                                newFiles.Add(lineKey[1]);
                                if (fixityFileData.ContainsKey(lineKey[0]))
                                {
                                    fixityFileData[lineKey[0]] = newFiles;
                                }
                                else
                                {
                                    fixityFileData.Add(lineKey[0], newFiles);
                                }

                                fixityDataObj.AddDataToNewFilesList(newFiles);
                            }
                            else if (line.StartsWith("Confirmed File:"))
                            {
                                string pattern = @"\t";
                                string[] lineKey = Regex.Split(line, pattern);
                                confirmedFiles.Add(lineKey[1]);
                                if (fixityFileData.ContainsKey(lineKey[0]))
                                {
                                    fixityFileData[lineKey[0]] = confirmedFiles;
                                }
                                else
                                {
                                    fixityFileData.Add(lineKey[0], confirmedFiles);
                                }

                                fixityDataObj.AddDataToConfirmedFilesList(confirmedFiles);
                            }
                        }
                        hasParsed = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                fixityFilesCollection.Add(Path.GetFileName(fileName), fixityFileData);

                fixityDataList.Add(fixityDataObj);
            }

            //Populating project name combo box after Fixity files have been parsed
            List<string> tempProjectNames = new List<string>();
            foreach (FixityData obj in fixityDataList)
            {
                tempProjectNames.Add(obj.projectName);
            }
            mainWindow.projectNamesCB.ItemsSource = tempProjectNames;
            
        }

        public void printProjectResult(string projectNameReceived)
        {
            foreach (FixityData obj in fixityDataList)
            {
                if (projectNameReceived == obj.projectName)
                {
                    mainWindow.projectNameTB.Text = obj.projectName;
                    mainWindow.totalFilesTB.Text = obj.totalFiles;
                    mainWindow.confirmedFilesTB.Text = obj.confirmedFiles;
                    mainWindow.movedOrRenamedTB.Text = obj.movedOrRenamedFiles;
                    mainWindow.newFilesTB.Text = obj.newFiles;
                    mainWindow.changedFilesTB.Text = obj.changedFiles;
                    if (obj.GetRemovedFilesList().Count > 0)
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("Removed Files");
                        foreach (string data in obj.GetRemovedFilesList())
                        {
                            dt.Rows.Add(new object[] { "" + data });
                        }

                        mainWindow.dgRemovedFilesList.ItemsSource = dt.DefaultView;
                    }
                    else
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("No Removed Files");
                        dt.Rows.Add(new object[] { "No data" });
                        mainWindow.dgRemovedFilesList.ItemsSource = dt.DefaultView;
                    }
                    if (obj.GetChangedFilesList().Count > 0)
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("Changed Files");
                        foreach (string data in obj.GetChangedFilesList())
                        {
                            dt.Rows.Add(new object[] { "" + data });
                        }

                        mainWindow.dgChangedFilesList.ItemsSource = dt.DefaultView;
                    }
                    else
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("No Changed Files");
                        dt.Rows.Add(new object[] { "No data" });
                        mainWindow.dgChangedFilesList.ItemsSource = dt.DefaultView;
                    }
                    if (obj.GetNewFilesList().Count > 0)
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("New Files");
                        foreach (string data in obj.GetNewFilesList())
                        {
                            dt.Rows.Add(new object[] { "" + data });
                        }

                        mainWindow.dgNewFilesList.ItemsSource = dt.DefaultView;
                    }
                    else
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("No New Files");
                        dt.Rows.Add(new object[] { "No data" });
                        mainWindow.dgNewFilesList.ItemsSource = dt.DefaultView;
                    }
                    if (obj.GetConfirmedFilesList().Count > 0)
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("Confirmed Files");
                        foreach (string data in obj.GetConfirmedFilesList())
                        {
                            dt.Rows.Add(new object[] { "" + data });
                        }

                        mainWindow.dgConfirmedFilesList.ItemsSource = dt.DefaultView;
                    }
                    else
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("No Confirmed Files");
                        dt.Rows.Add(new object[] { "No data" });
                        mainWindow.dgConfirmedFilesList.ItemsSource = dt.DefaultView;
                    }
                }
            }
        }
    }
}
