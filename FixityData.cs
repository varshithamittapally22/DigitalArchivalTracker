using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalArchivalTracker
{
    class FixityData
    {
        public string projectName { get; set; }

        public string totalFiles { get; set; }

        public string confirmedFiles { get; set; }

        public string movedOrRenamedFiles { get; set; }

        public string newFiles { get; set; }

        public string changedFiles { get; set; }

        List<string> removedFilesList = new List<string>();
        List<string> changedFilesList = new List<string>();
        List<string> newFilesList = new List<string>();
        List<string> confirmedFilesList = new List<string>();

        public void AddDataToRemovedFilesList(List<string> removedFilesListReceived)
        {
            removedFilesList = removedFilesListReceived;
        }

        public List<string> GetRemovedFilesList()
        {
            return removedFilesList;
        }

        public void AddDataToChangedFilesList(List<string> changedFilesListReceived)
        {
            changedFilesList = changedFilesListReceived;
        }

        public List<string> GetChangedFilesList()
        {
            return changedFilesList;
        }

        public void AddDataToNewFilesList(List<string> newFilesListReceived)
        {
            newFilesList = newFilesListReceived;
        }

        public List<string> GetNewFilesList()
        {
            return newFilesList;
        }

        public void AddDataToConfirmedFilesList(List<string> confirmedFilesListReceived)
        {
            confirmedFilesList = confirmedFilesListReceived;
        }

        public List<string> GetConfirmedFilesList()
        {
            return confirmedFilesList;
        }

        override
        public String ToString()
        {
            return "Project Name: " + projectName + ", Total Files: " + totalFiles + ", Confirmed Files: " + confirmedFiles + ", Moved/Renamed: " + movedOrRenamedFiles +
                ", New Files: " + newFiles + ", Changed Files: " + changedFiles;
        }
    }
}
