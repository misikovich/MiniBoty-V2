using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MiniBoty
{
    class SRPData
    {
        public SRPData(string FileName, char Separator)
        {
            _filepath = FileName;
            _separator = Separator;
            ReadData();
        }

        List<string> Lines = new();
        List<string> ParameterNames = new();
        List<string> ParameterValues = new();
        private char _separator;
        private string _filepath;
        private void ReadData()
        {
            StreamWriter sw = File.AppendText(_filepath);
            sw.Close();

            Lines.Clear();
            ParameterNames.Clear();
            ParameterValues.Clear();

            foreach (string line in File.ReadLines(_filepath))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Lines.Add(line);
                }
            }

            foreach (string line in Lines)
            {
                char[] temp = line.ToCharArray();
                string _pName = ""; string _pValue = "";
                bool isName = true;

                foreach (char item in temp)
                {
                    if (isName && item != _separator) { _pName += item; }
                    else if (isName && item == _separator) { isName = false; }
                    else if (!isName) { _pValue += item; }
                }

                ParameterNames.Add(_pName);
                if (!string.IsNullOrEmpty(_pValue)) { ParameterValues.Add(_pValue); }
                else { ParameterValues.Add(item: "null"); }
            }
        }
        public void SaveData(string[] parameters, string[] values)
        {
            if (parameters.Length > 0 && values.Length > 0)
            {
                ReadData();
                string[] AllLines = File.ReadAllLines(_filepath);
                int currentTransferredLine = 0;
                if (ParameterNames.Count < 1)
                {
                    using (StreamWriter sw = new StreamWriter(_filepath))
                    {
                        sw.WriteLine(parameters[currentTransferredLine] + _separator + values[currentTransferredLine]);
                    }
                    parameters = parameters.Where((source, index) => index != 0).ToArray();
                    values = values.Where((source, index) => index != 0).ToArray();
                    AllLines = File.ReadAllLines(_filepath);
                }
                foreach (string transferParameter in parameters)
                {
                    int сurrentFileLine = 0;
                    bool isExist = false;
                    foreach (string fileParameter in ParameterNames)
                    {
                        if (transferParameter == fileParameter)
                        {
                            AllLines[сurrentFileLine] = transferParameter + _separator + values[currentTransferredLine];
                            isExist = true;
                            goto breakpoint;
                        }
                        сurrentFileLine++;
                    }
                breakpoint:;
                    if (!isExist)
                    {
                        Array.Resize(ref AllLines, AllLines.Length + 1);
                        AllLines[AllLines.Length - 1] = transferParameter + _separator + values[currentTransferredLine];
                    }
                    currentTransferredLine++;
                }
                File.WriteAllLines(_filepath, AllLines);
                ReadData();
            }
        }
        public string ParseDataString(string parameter)
        {
            string value;
            if (ParameterNames.Contains(parameter))
            {
                int index = ParameterNames.FindIndex(a => a.Contains(parameter));
                value = ParameterValues[index];
                return value;
            }
            else { return null; }
        }
        public int ParseDataInt(string parameter)
        {
            int value;
            if (ParameterNames.Contains(parameter))
            {
                int index = ParameterNames.FindIndex(a => a.Contains(parameter));
                try { value = Convert.ToInt32(ParameterValues[index]); }
                catch (ArgumentException) { value = 0; }

                return value;
            }
            else { return 0; }
        }
        public void DeleteParameter(string[] parameters)
        {
            ReadData();
            foreach (var item in parameters)
            {
                if (ParameterNames.Contains(item))
                {
                    int index = ParameterNames.FindIndex(a => a.Contains(item));
                    ParameterNames.RemoveAt(index);
                    ParameterValues.RemoveAt(index);
                    Lines.RemoveAt(index);
                }
            }
            string[] pn_array = ParameterNames.ToArray();
            string[] pv_array = ParameterValues.ToArray();
            File.Delete(_filepath);
            SaveData(pn_array, pv_array);
        }
    }
}
