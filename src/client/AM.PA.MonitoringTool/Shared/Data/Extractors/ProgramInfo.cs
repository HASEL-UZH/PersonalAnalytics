// Created by André Meyer at MSR
// Created: 2015-12-03
// 
// Licensed under the MIT License.
using System.Collections.Generic;

namespace Shared.Data.Extractors
{
    public class ProgramInfo
    {
        public ProgramInfo(string pN, string r)
        {
            ProcessName = pN;
            FileExtensions = new List<string> { };
            RemovablesRegex = new List<string> { r };
        }

        public ProgramInfo(string pN, List<string> r)
        {
            ProcessName = pN;
            FileExtensions = new List<string>();
            RemovablesRegex = r;
        }

        public ProgramInfo(string pN, string fE, string r)
        {
            ProcessName = pN;
            FileExtensions = new List<string> { fE };
            RemovablesRegex = new List<string> { r };
        }

        public ProgramInfo(string pN, List<string> fE, string r)
        {
            ProcessName = pN;
            FileExtensions = fE;
            RemovablesRegex = new List<string> { r };
        }

        public ProgramInfo(string pN, string fE, List<string> r)
        {
            ProcessName = pN;
            FileExtensions = new List<string> { fE };
            RemovablesRegex = r;
        }

        public ProgramInfo(string pN, List<string> fE, List<string> r)
        {
            ProcessName = pN;
            FileExtensions = fE;
            RemovablesRegex = r;
        }

        public string ProcessName { get; set; }
        public List<string> FileExtensions { get; set; }
        public List<string> RemovablesRegex { get; set; }
    }
}
