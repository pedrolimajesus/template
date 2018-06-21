using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using Microsoft.CSharp.RuntimeBinder;

namespace Shrike.Areas.GlobalUI.GlobalUI.Components
{
    public class WebGridExtend : WebGrid
    {
        private bool _canPage;

        public WebGridExtend(IEnumerable<dynamic> source = null,
                                IEnumerable<string> columnNames = null,
                                string defaultSort = null,
                                int rowsPerPage = 10,
                                bool canPage = true,
                                bool canSort = true,
                                string ajaxUpdateContainerId = null,
                                string ajaxUpdateCallback = null,
                                string fieldNamePrefix = null,
                                string pageFieldName = null,
                                string selectionFieldName = null,
                                string sortFieldName = null,
                                string sortDirectionFieldName = null)
            : base(
                    source,
                    columnNames,
                    defaultSort,
                    rowsPerPage,
                    canPage = true,
                    canSort,
                    ajaxUpdateContainerId,
                    ajaxUpdateCallback,
                    fieldNamePrefix,
                    pageFieldName,
                    selectionFieldName,
                    sortFieldName,
                    sortDirectionFieldName)
        {
            _canPage = canPage;
        }

        public IHtmlString GetHtml(
                                    string tableStyle = null,
                                    string headerStyle = null,
                                    string footerStyle = null,
                                    string rowStyle = null,
                                    string alternatingRowStyle = null,
                                    string selectedRowStyle = null,
                                    string caption = null,
                                    bool displayHeader = true,
                                    bool fillEmptyRows = false,
                                    string emptyRowCellValue = null,
                                    IEnumerable<WebGridExtendColumn> columns = null,
                                    IEnumerable<string> exclusions = null,
                                    WebGridPagerModes mode = WebGridPagerModes.NextPrevious | WebGridPagerModes.Numeric,
                                    string firstText = null,
                                    string previousText = null,
                                    string nextText = null,
                                    string lastText = null,
                                    int numericLinksCount = 5,
                                    object htmlAttributes = null,
                                    bool displayTotal = false,
                                    string totalRowStyle = null)
        {
            Func<dynamic, object> footer = null;
            if (_canPage && (PageCount > 1))
            {
                footer = item => Pager(mode, firstText, previousText, nextText, lastText, numericLinksCount);
            }

            return Table(tableStyle, headerStyle, footerStyle, rowStyle, alternatingRowStyle, selectedRowStyle, caption,
                         displayHeader,
                         fillEmptyRows, emptyRowCellValue, columns, exclusions, displayTotal, totalRowStyle,
                         footer: footer,
                         htmlAttributes: htmlAttributes);
        }

        public IHtmlString Table(
                                    string tableStyle = null,
                                    string headerStyle = null,
                                    string footerStyle = null,
                                    string rowStyle = null,
                                    string alternatingRowStyle = null,
                                    string selectedRowStyle = null,
                                    string caption = null,
                                    bool displayHeader = true,
                                    bool fillEmptyRows = false,
                                    string emptyRowCellValue = null,
                                    IEnumerable<WebGridExtendColumn> columns = null,
                                    IEnumerable<string> exclusions = null,
                                    bool displayTotal = false,
                                    string totalRowStyle = null,
                                    Func<dynamic, object> footer = null,
                                    object htmlAttributes = null)
        {
            IHtmlString html;
            if (displayTotal)
            {
                string totalRow = BuildTotalRow(this.Rows, totalRowStyle, columns);
                string baseHtml =
                    base.Table(tableStyle, headerStyle, footerStyle, rowStyle, alternatingRowStyle, selectedRowStyle,
                               caption, displayHeader, fillEmptyRows, emptyRowCellValue, columns, exclusions, footer,
                               htmlAttributes).ToString();
                string htmlWithTotal = baseHtml.Replace("</tbody></table>", totalRow);

                html = new HtmlString(htmlWithTotal);
            }
            else
            {
                html = base.Table(tableStyle, headerStyle, footerStyle, rowStyle, alternatingRowStyle, selectedRowStyle,
                                  caption, displayHeader, fillEmptyRows, emptyRowCellValue, columns, exclusions, footer,
                                  htmlAttributes);
            }

            return html;
        }

        private string BuildTotalRow(IList<WebGridRow> rows, string totalRowStyle, IEnumerable<WebGridExtendColumn> columns)
        {
            var rowTag = string.Format("<tr class=\"{0}\">", totalRowStyle);

            StringBuilder rowStringBuilder = new StringBuilder(rowTag);
            foreach (var column in columns)
            {
                //Check if this column should be totaled
                if (column.Total)
                {
                    //Sum the column by looping through each row, getting the value, and adding it to the total
                    decimal columnTotal = 0;
                    foreach (var row in rows)
                    {
                        decimal cellValue = 0;
                        try
                        {
                            //Get the value in the column for this particular row
                            Type rowType = row.Value.GetType();
                            PropertyInfo rowProperty = rowType.GetProperty(column.ColumnName);
                            var propertyValue = rowProperty.GetValue(row.Value, null);
                            decimal.TryParse(propertyValue.ToString(), out cellValue);
                        }
                        //If exception is RuntimBinderException most likely the value is null and can't be summed anyway, so move on
                        catch (RuntimeBinderException) { }

                        columnTotal += cellValue;
                    }

                    //Format the total based on the user specified format
                    string totalFormat = column.TotalFormat == null ? "{0:0.00}" : column.TotalFormat;
                    var formattedTotal = string.Format(totalFormat, columnTotal);

                    //Create a new table cell tag containing the total and append it to the output string
                    var td = new TagBuilder("td");
                    td.MergeAttribute("class", column.TotalStyle);
                    td.InnerHtml = formattedTotal;

                    rowStringBuilder.Append(td.ToString());
                }
                else
                {
                    //If the column shouldn't be totaled just append an empty table cell
                    var td = new TagBuilder("td");
                    rowStringBuilder.Append(td.ToString());
                }
            }
            rowStringBuilder.Append("</tr></tbody></table>");

            return rowStringBuilder.ToString();
        }

        public WebGridRow SelectedRowDetails()
        {
            if (this.HasSelection)
            {
                return this.Rows[this.SelectedIndex];
            }
            return null;
        }
    }


    public class WebGridExtendColumn : WebGridColumn
    {
        public bool Total { get; set; }
        public string TotalFormat { get; set; }
        public string TotalStyle { get; set; }
    }

   
}