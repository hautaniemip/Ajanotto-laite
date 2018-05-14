using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ajanottolaite
{
    public static class DataTableExtensions
    {
        public static void WriteCsvFile(this DataTable dataTable, string filePath)
        {
            StringBuilder fileContent = new StringBuilder();

            foreach (var col in dataTable.Columns)
            {
                fileContent.Append(col.ToString() + ",");
            }

            fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);

            foreach (DataRow dr in dataTable.Rows)
            {
                foreach (var column in dr.ItemArray)
                {
                    fileContent.Append("\"" + column.ToString() + "\",");
                }

                fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
            }

            System.IO.File.WriteAllText(filePath, fileContent.ToString());
        }


        public static void ConvertCSVtoDataTable(this DataTable dataTable, string strFilePath)
        {
            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string[] headers = sr.ReadLine().Split(',');    // Expects first row as header
                foreach (string header in headers)
                {
                    try
                    {
                        dataTable.Columns.Add(header);          // Adds headers to DataTable, 
                                                                // but doesn't add if DataTable already contains header with same name
                    }
                    catch (DuplicateNameException)
                    {
                        continue;
                    }

                }
                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(',');
                    DataRow dr = dataTable.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];                        // Create data for a row
                    }
                    dataTable.Rows.Add(dr);                     // Add data to rows
                }
            }
        }
    }
}
