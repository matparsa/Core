﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BExIS.Io.Transform.Validation.DSValidation;
using BExIS.Io.Transform.Validation.Exceptions;
using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Entities.DataStructure;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;



namespace BExIS.Io.Transform.Input
{
    /// <summary>
    /// http://blogs.msdn.com/b/brian_jones/archive/2010/05/27/parsing-and-reading-large-excel-files-with-the-open-xml-sdk.aspx
    /// </summary>

    /// <summary>
    /// this class is used to read and validate excel files
    /// </summary>
    /// <remarks></remarks>     
    public class ExcelReader:DataReader
    {
        private SharedStringItem[] _sharedStrings;
        private Stylesheet _stylesheet = new Stylesheet();
        SpreadsheetDocument spreadsheetDocument;
        private Worksheet worksheet;
        private DefinedNameVal _areaOfData = new DefinedNameVal();
        private DefinedNameVal _areaOfVariables = new DefinedNameVal();

        // read data 
        int startColumn = 0;
        int endColumn = 0;
        int numOfColumns = 0;
        int offset = 0;

        int rows = 0;

        private char[] alphabet = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

        #region read file


        /// <summary>
        /// return true if the Excel file based on a template
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="file"> file as stream</param>       
        public bool IsTemplate(Stream file)
        { 
            this.file = file;

            // open excel file
            spreadsheetDocument = SpreadsheetDocument.Open(this.file, false);
        
            // get workbookpart
            WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;

            
            try
            {
                // get all the defined area 
                List<DefinedNameVal> namesTable = BuildDefinedNamesTable(workbookPart);

                if (namesTable.Where(p => p.Key.Equals("Data")).FirstOrDefault() != null
                    && namesTable.Where(p => p.Key.Equals("VariableIdentifiers")).FirstOrDefault() != null)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
            

            return false;
        }


        /// <summary>
        /// Read a Excel Template file
        /// Convert the rows into a datatuple based on the datastructure.
        /// Return a list of datatuples
        /// </summary>
        /// <remarks>Only when excel template is in use</remarks>
        /// <seealso cref=""/>
        /// <param name="file">File as stream</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="sds">StructuredDataStructure of a dataset</param>
        /// <param name="datasetId">Datasetid of a dataset</param>
        /// <returns>List of DataTuples</returns>
        public List<DataTuple> ReadFile(Stream file, string fileName, StructuredDataStructure sds, long datasetId)
        {

            this.file = file;
            this.fileName = fileName;

            this.structuredDataStructure = sds;
            //this.info = efri;
            this.datasetId = datasetId;
                        
            // open excel file
            spreadsheetDocument = SpreadsheetDocument.Open(this.file, false);

            // get workbookpart
            WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;

            // get all the defined area 
            List<DefinedNameVal> namesTable = BuildDefinedNamesTable(workbookPart);


            // select data area
            this._areaOfData = namesTable.Where(p => p.Key.Equals("Data")).FirstOrDefault();

            // Select variable area
            this._areaOfVariables = namesTable.Where(p => p.Key.Equals("VariableIdentifiers")).FirstOrDefault();

            // Get intergers for reading data
            startColumn = GetColumnNumber(this._areaOfData.StartColumn);
            endColumn = GetColumnNumber(this._areaOfData.EndColumn);

            numOfColumns = (endColumn - startColumn) + 1;
            offset = GetColumnNumber(GetColumnName(this._areaOfData.StartColumn)) - 1;

            // select worksheetpart by selected defined name area like data in sheet
            // sheet where data area is inside
            WorksheetPart worksheetPart = GetWorkSheetPart(workbookPart, this._areaOfData);
            //worksheet = worksheetPart.Worksheet;
            // get styleSheet
            _stylesheet = workbookPart.WorkbookStylesPart.Stylesheet;

            // Get shared strings
            _sharedStrings = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ToArray();

            if (GetSubmitedVariableIdentifier(worksheetPart, this._areaOfVariables.StartRow, this._areaOfVariables.EndRow)!=null)
            {
                ReadRows(worksheetPart, this._areaOfData.StartRow, this._areaOfData.EndRow);
            }

            return this.dataTuples;
        }


       

        /// <summary>
        /// Read a Excel row by row
        /// Convert the rows into a datatuple based on the datastructure.
        /// Return a list of datatuples
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="file">File as stream</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="fri">FileReaderInfo (ExcelFileReaderInfo) for additional Informations to read the file</param>
        /// <param name="sds">StructuredDataStructure of a dataset</param>
        /// <param name="datasetId">Datasetid of a dataset</param>
        /// <returns>List of DataTuples</returns>
        public List<DataTuple> ReadFile(Stream file, string fileName, FileReaderInfo fri, StructuredDataStructure sds, long datasetId)
        {
            this.file = file;
            this.fileName = fileName;

            this.structuredDataStructure = sds;
            this.info = fri;
            this.datasetId = datasetId;

             // Check params
            if (this.file == null)
            {
                this.errorMessages.Add(new Error(ErrorType.Other, "File not exist"));
            }
            if (!this.file.CanRead)
            {
                this.errorMessages.Add(new Error(ErrorType.Other, "File is not readable"));
            }
            if (this.info.Variables <= 0)
            {
                this.errorMessages.Add(new Error(ErrorType.Other, "Startrow of Variable can´t be 0"));
            }
            if (this.info.Data <= 0)
            {
                this.errorMessages.Add(new Error(ErrorType.Other, "Startrow of Data can´t be 0"));
            }

            if (this.errorMessages.Count == 0)
            {
                // open excel file
                spreadsheetDocument = SpreadsheetDocument.Open(this.file, false);

                // get workbookpart
                WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;

                SheetDimension dimension = workbookPart.WorksheetParts.First().Worksheet.GetFirstChild<SheetDimension>();

               string s =  dimension.Reference.Value;

               string[] references = s.Split(':'); 
                 

                // get all the defined area 
                //List<DefinedNameVal> namesTable = BuildDefinedNamesTable(workbookPart);


                // Get intergers for reading data
                startColumn = GetColumnNumber(GetColumnName(references[0]));
                endColumn = GetColumnNumber(GetColumnName(references[1]));
                
                numOfColumns = (endColumn - startColumn) + 1;
                offset = this.info.Offset;

                int endRowData = GetRowNumber(references[1]);
                
                // select worksheetpart by selected defined name area like data in sheet
                // sheet where data area is inside
                WorksheetPart worksheetPart = workbookPart.WorksheetParts.First(); //GetWorkSheetPart(workbookPart, this._areaOfData);

                // get styleSheet
                _stylesheet = workbookPart.WorkbookStylesPart.Stylesheet;

                // Get shared strings
                _sharedStrings = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ToArray();

                if (GetSubmitedVariableIdentifier(worksheetPart, this.info.Variables, this.info.Variables) != null)
                {
                    ReadRows(worksheetPart, this.info.Data, endRowData);
                }

                return this.dataTuples;


            }

            return this.dataTuples;
        }

        /// <summary>
        /// Read rows from worksheetPart starts from a startrow and ends on the endrow
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="worksheetPart">part of a excel worksheet</param>       
        /// <param name="startRow">start row</param>       
        /// <param name="endRow">end row</param>       
        protected void ReadRows(WorksheetPart worksheetPart, int startRow, int endRow)
        {
            OpenXmlReader reader = OpenXmlReader.Create(worksheetPart);
            int count = 0;
            int rowNum = 0;

            while (reader.Read())
            {
                if (reader.ElementType == typeof(Row))
                {
                    do
                    {
                        if (reader.HasAttributes)
                            rowNum = Convert.ToInt32(reader.Attributes.First(a => a.LocalName == "r").Value);

                        if (endRow == 0)
                        {
                            if (rowNum >= startRow)
                            {
                                Row row = (Row)reader.LoadCurrentElement();

                                //this.errorMessages = this.errorMessages.Union(Validate(RowToList(row), Convert.ToInt32(row.RowIndex.ToString()))).ToList();
                                this.dataTuples.Add(ReadRow(RowToList(row), Convert.ToInt32(row.RowIndex.ToString())));
                                count++;
                            }
                        }
                        else
                        {
                            if (rowNum >= startRow && rowNum <= endRow)
                            {
                                Row row = (Row)reader.LoadCurrentElement();

                                //this.errorMessages = this.errorMessages.Union(Validate(RowToList(row), Convert.ToInt32(row.RowIndex.ToString()))).ToList();
                                this.dataTuples.Add(ReadRow(RowToList(row), Convert.ToInt32(row.RowIndex.ToString())));
                                count++;

                            }
                        }


                    } while (reader.ReadNextSibling()); // Skip to the next row

                    break;

                }

            }
        }

        #endregion

        #region validate

        /// <summary>
        /// Validate a Excel Template file
        /// </summary>
        /// <remarks>Only when excel template is in use</remarks>
        /// <seealso cref=""/>
        /// <param name="file">File as stream</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="sds">StructuredDataStructure of a dataset</param>
        /// <param name="datasetId">Datasetid of a dataset</param>
        public void ValidateFile(Stream file, string fileName, StructuredDataStructure sds, long datasetId)
        {
            this.file = file;
            this.fileName = fileName;

            this.structuredDataStructure = sds;
            //this.info = efri;
            this.datasetId = datasetId;

            // open excel file
            spreadsheetDocument = SpreadsheetDocument.Open(this.file, false);

            // get workbookpart
            WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;

            // get all the defined area 
            List<DefinedNameVal> namesTable = BuildDefinedNamesTable(workbookPart);

            // select data area
            this._areaOfData = namesTable.Where(p => p.Key.Equals("Data")).FirstOrDefault();
            if(this._areaOfData == null) this.errorMessages.Add(new Error(ErrorType.Other,"Data area is not defined in the excel template."));

            // Select variable area
            this._areaOfVariables = namesTable.Where(p => p.Key.Equals("VariableIdentifiers")).FirstOrDefault();
            if(this._areaOfVariables == null) this.errorMessages.Add(new Error(ErrorType.Other,"VariableIdentifiers area is not defined in the excel template."));

            // Get intergers for reading data
            startColumn = GetColumnNumber(this._areaOfData.StartColumn);
            endColumn = GetColumnNumber(this._areaOfData.EndColumn);

            numOfColumns = (endColumn - startColumn) + 1;
            offset = GetColumnNumber(GetColumnName(this._areaOfData.StartColumn)) - 1;

            // select worksheetpart by selected defined name area like data in sheet
            // sheet where data area is inside
            WorksheetPart worksheetPart = GetWorkSheetPart(workbookPart, this._areaOfData);
            //worksheet = worksheetPart.Worksheet;
            // get styleSheet
            _stylesheet = workbookPart.WorkbookStylesPart.Stylesheet;

            // Get shared strings
            _sharedStrings = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ToArray();

            if (this.errorMessages.Count == 0)
            {
                if (ValidateDatastructure(worksheetPart, this._areaOfVariables.StartRow, this._areaOfVariables.EndRow))
                {
                    ValidateRows(worksheetPart, this._areaOfData.StartRow, this._areaOfData.EndRow);
                }
            }
            // close fehlt
        }


        /// <summary>
        /// Validate Datastructure from file
        /// true = valid
        /// false = not valid
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="worksheetPart">Part of a excel worksheet where the datastructure is located</param>       
        /// <param name="startRow">Row where the DataStructure is starting</param>       
        /// <param name="endRow">Row where the DataStructure is ending</param>       
        protected bool ValidateDatastructure(WorksheetPart worksheetPart, int startRow, int endRow)
        {
            List<Error> errorList;

            errorList = ValidateComparisonWithDatatsructure(GetSubmitedVariableIdentifier(worksheetPart, startRow, endRow));

            if (errorList != null)
            {
                if (errorList.Count > 0)
                {
                    this.errorMessages = this.errorMessages.Concat(errorList).ToList();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validate rows from file
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="worksheetPart">Part of a excel worksheet where the datastructure is located</param>       
        /// <param name="startRow">Rows starting</param>       
        /// <param name="endRow">Rows ending</param>   
        protected void ValidateRows(WorksheetPart worksheetPart, int startRow, int endRow)
        {
            //NEW OPENXMLREADER

            OpenXmlReader reader = OpenXmlReader.Create(worksheetPart);
            int count = 0;
            int rowNum = 0;

            while (reader.Read())
            {
                if (reader.ElementType == typeof(Row))
                {
                    do
                    {
                        if (reader.HasAttributes)
                            rowNum = Convert.ToInt32(reader.Attributes.First(a => a.LocalName == "r").Value);

                        if (rowNum >= startRow && rowNum <= endRow)
                        {
                            Row row = (Row)reader.LoadCurrentElement();

                            this.errorMessages = this.errorMessages.Union(ValidateRow(RowToList(row), rowNum)).ToList();
                            count++;


                        }
                    } while (reader.ReadNextSibling()); // Skip to the next row

                    break;

                }

            }
        }

        #endregion

        #region helper methods


        /// <summary>
        /// Convert a excel row to a list of strings
        /// Every cell is one value
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="r"> row from a excel file</param>  
        /// <returns>list of string for each cell in the row</returns>
        private List<string> RowToList(Row r)
        {

            string[] rowAsStringArray = new string[numOfColumns];

            // create a new cell
            Cell c = new Cell();

            for (int i = 0; i < r.ChildElements.Count(); i++)
            {
                // get current cell at i
                c = r.Elements<Cell>().ElementAt(i);

                string value = "";

                
                if (c != null)
                {
                    if (c.CellValue != null)
                    {
                        // if cell reference in range of the area
                        int cellReferencAsInterger = GetColumnNumber(GetColumnName(c.CellReference));
                        int start = GetColumnNumber(this._areaOfData.StartColumn);
                        int end = GetColumnNumber(this._areaOfData.EndColumn);

                        if (cellReferencAsInterger >= start && cellReferencAsInterger<=end)
                        {

                            // if Value a text
                            if (c.DataType != null && c.DataType.HasValue && c.DataType.Value == CellValues.SharedString)
                            {
                                int sharedStringIndex = int.Parse(c.CellValue.Text, CultureInfo.InvariantCulture);
                                SharedStringItem sharedStringItem = _sharedStrings[sharedStringIndex];
                                value = sharedStringItem.InnerText;

                            }
                            // not a text
                            else if (c.StyleIndex != null && c.StyleIndex.HasValue)
                            {
                                uint styleIndex = c.StyleIndex.Value;
                                CellFormat cellFormat = _stylesheet.CellFormats.ChildElements[(int)styleIndex] as CellFormat;
                                if (cellFormat.ApplyNumberFormat != null && cellFormat.ApplyNumberFormat.HasValue && cellFormat.ApplyNumberFormat.Value && cellFormat.NumberFormatId != null && cellFormat.NumberFormatId.HasValue)
                                {
                                    uint numberFormatId = cellFormat.NumberFormatId.Value;

                                        // Number format 14-22 and 45-47 are built-in date and/or time formats
                                        if ((numberFormatId >= 14 && numberFormatId <= 22) || (numberFormatId >= 45 && numberFormatId <= 47))
                                        {
                                            DateTime dateTime = DateTime.FromOADate(double.Parse(c.CellValue.Text, CultureInfo.InvariantCulture));
                                            value = dateTime.ToString();
                                        }
                                        else
                                        {
                                            if (_stylesheet.NumberingFormats != null && _stylesheet.NumberingFormats.Any(numFormat => ((NumberingFormat)numFormat).NumberFormatId.Value == numberFormatId))
                                            {
                                                NumberingFormat numberFormat = _stylesheet.NumberingFormats.First(numFormat => ((NumberingFormat)numFormat).NumberFormatId.Value == numberFormatId) as NumberingFormat;

                                                if (numberFormat != null && numberFormat.FormatCode != null && numberFormat.FormatCode.HasValue)
                                                {
                                                    string formatCode = numberFormat.FormatCode.Value;
                                                    if ((formatCode.Contains("h") && formatCode.Contains("m")) || (formatCode.Contains("m") && formatCode.Contains("d")))
                                                    {
                                                        DateTime dateTime = DateTime.FromOADate(double.Parse(c.CellValue.Text, CultureInfo.InvariantCulture));
                                                        value = dateTime.ToString();

                                                    }
                                                    else
                                                    {
                                                        value = c.CellValue.Text;
                                                    }
                                                }
                                                else
                                                {
                                                    value = c.CellValue.Text;
                                                }
                                            }
                                            else
                                            {
                                                value = c.CellValue.Text;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        value = c.CellValue.Text;
                                    }

                                }
                        
                            // define index based on cell refernce - offset 
                            int index = cellReferencAsInterger - offset-1;
                            rowAsStringArray[index] = value;
                        }
                    }//end if cell value
                }//end if cell null

            }//for

            return rowAsStringArray.ToList();
        }

        /// <summary>
        /// Generate a list of VariableIdentifiers from the excel file
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="worksheetPart">part of a excel worksheet</param>       
        /// <param name="startRow">start row</param>       
        /// <param name="endRow">end row</param> 
        /// <returns>List of variableIdentifier</returns>
        private List<VariableIdentifier> GetSubmitedVariableIdentifier(WorksheetPart worksheetPart, int startRow, int endRow)
        {
            //NEW OPENXMLREADER
            if (this.SubmitedVariableIdentifiers == null || this.SubmitedVariableIdentifiers.Count == 0)
            {

                OpenXmlReader reader = OpenXmlReader.Create(worksheetPart);
                int rowNum = 0;

                // read variable rows to get name and id from area variable
                while (reader.Read())
                {
                    if (reader.ElementType == typeof(Row))
                    {
                        do
                        {
                            if (reader.HasAttributes)
                                rowNum = Convert.ToInt32(reader.Attributes.First(a => a.LocalName == "r").Value);

                            if (rowNum >= startRow && rowNum <= endRow)
                            {
                                Row row = (Row)reader.LoadCurrentElement();

                                if (row.Hidden == null) variableIdentifierRows.Add(RowToList(row));
                                else if (row.Hidden != true) variableIdentifierRows.Add(RowToList(row));

                            }

                        } while (reader.ReadNextSibling() && rowNum < endRow); // Skip to the next row
                        break;
                    }
                }

                // convert variable rows to VariableIdentifiers
                if (variableIdentifierRows != null)
                {
                    foreach (List<string> l in variableIdentifierRows)
                    {
                        //create headerVariables
                        if (SubmitedVariableIdentifiers.Count == 0)
                        {
                            foreach (string s in l)
                            {
                                VariableIdentifier hv = new VariableIdentifier();
                                hv.name = s;
                                SubmitedVariableIdentifiers.Add(hv);
                            }
                        }
                        else
                        {
                            foreach (string s in l)
                            {
                                int id = Convert.ToInt32(s);
                                int index = l.IndexOf(s);
                                SubmitedVariableIdentifiers.ElementAt(index).id = id;
                            }
                        }
                    }
                }
            }

            if (this.SubmitedVariableIdentifiers != null) return this.SubmitedVariableIdentifiers;
            else return null;
        }


        /// <summary>
        /// generate a list of DefinedNameVal which includes all information
        /// about areas in a Excel template file like Data and VariableIdentifiers from the excel file
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="workbookPart">part of the workbook</param>       
        private List<DefinedNameVal> BuildDefinedNamesTable(WorkbookPart workbookPart)
        {
            //Build a list
            List<DefinedNameVal> definedNames = new List<DefinedNameVal>();
            foreach (DefinedName name in workbookPart.Workbook.GetFirstChild<DefinedNames>())
            {
                //Parse defined name string...
                string key = name.Name;

                if (key.Equals("Data") || key.Equals("VariableIdentifiers"))
                {
                    string reference = name.InnerText;
                    string sheetName = reference.Split('!')[0];
                    sheetName = sheetName.Trim('\'');

                    //Assumption: None of my defined names are relative defined names (i.e. A1)
                    string range = reference.Split('!')[1];
                    string[] rangeArray = range.Split('$');
                    string startCol = rangeArray[1];
                    int startRow = Convert.ToInt32(rangeArray[2].TrimEnd(':'));
                    string endCol = null;
                    int endRow = 0;
                    if (rangeArray.Length > 3)
                    {
                        endCol = rangeArray[3];
                        endRow = Convert.ToInt32(rangeArray[4]);
                    }
                    definedNames.Add(new DefinedNameVal() { Key = key, SheetName = sheetName, StartColumn = startCol, StartRow = startRow, EndColumn = endCol, EndRow = endRow });
                }
            }

            return definedNames;
        }


        /// <summary>
        /// Return the worksheet which include a specific area
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="workbookPart">Workbook part</param>    
        /// <param name="definedName">Object with informations about a area</param>
        /// <returns>Worksheet where the area is inside</returns>
        private static WorksheetPart GetWorkSheetPart(WorkbookPart workbookPart, DefinedNameVal definedName)
        {
                //get worksheet based on defined name
                string relId = workbookPart.Workbook.Descendants<Sheet>()
                .Where(s => definedName.SheetName.Equals(s.Name))
                .First()
                .Id;
                return (WorksheetPart)workbookPart.GetPartById(relId);
        }

        

        /// <summary>
        /// Get the cloumn name from a cell
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="cellName">Name of the cell</param>
        /// <returns>column name as string</returns>
        private static string GetColumnName(string cellName)
        {
            // Create a regular expression to match the column name portion of the cell name.
            Regex regex = new Regex("[A-Za-z]+");
            Match match = regex.Match(cellName);

            return match.Value;
        }

        /// <summary>
        /// Get the row number from a cell
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="cellName">Name of the cell</param>
        /// <returns>row number as int</returns>
        private static int GetRowNumber(string cellName)
        {
            // Create a regular expression to match the column name portion of the cell name.
            Regex regex = new Regex("[0-9]+");
            Match match = regex.Match(cellName);

            return Convert.ToInt32(match.Value);
        }


        /// <summary>
        /// Get Column number based on a column name
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref=""/>
        /// <param name="columnName"></param>    
        /// <returns>Column number as int</returns>
        private static int GetColumnNumber(string columnName)
        {
            char[] chars = columnName.ToCharArray();

            if (chars.Length == 1)
            {
                int colNr1 = (int)(chars[0] - 64);
                return colNr1;
            }
            else if (chars.Length == 2)
            {
                int colNr1 = (int)(chars[1] - 64);
                int colNr2 = (int)(chars[0] - 64);
                return colNr1 + (26 * colNr2);
            }
            else if (chars.Length == 3)
            {
                int colNr1 = (int)(chars[2] - 64);
                int colNr2 = (int)(chars[1] - 64);
                int colNr3 = (int)(chars[0] - 64);
                return colNr1 + (26 * colNr2) + (26 * 26 * colNr3);
            }
            else
            {
                return -1;
            }
        }

        #endregion

    }


    /// <summary>
    /// Represent a area in a excel file
    /// </summary>
    /// <remarks></remarks>        
    public class DefinedNameVal
    {
        public string Key ="";
        public string SheetName="";
        public string StartColumn="";
        public int StartRow=0;
        public string EndColumn="";
        public int EndRow=0;
    }

}