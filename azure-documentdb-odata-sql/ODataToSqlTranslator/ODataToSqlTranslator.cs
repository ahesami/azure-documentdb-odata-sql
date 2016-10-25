﻿using System;
using System.Linq;
using System.Web.OData.Query;

using Microsoft.OData.Core.UriParser;
using Microsoft.OData.Core.UriParser.Semantic;

namespace Microsoft.Azure.Documents.OData.Sql
{
    /// <summary>
    /// TranslateOptions
    /// </summary>
    public enum TranslateOptions
    {
        /// <summary>
        /// translate option for Sql SELECT clause
        /// </summary>
        SELECT_CLAUSE = 0x0001,

        /// <summary>
        /// translate option for Sql WHERE clause
        /// </summary>
        WHERE_CLAUSE = 0x0010,

        /// <summary>
        /// translate option for Sql ORDER BY clause
        /// </summary>
        ORDERBY_CLAUSE = 0x0100,

        /// <summary>
        /// translate option for sql TOP clause
        /// </summary>
        TOP_CLAUSE = 0x1000,

        /// <summary>
        /// translate option for all Sql clauses: SELECT, WHERE, ORDER BY, and TOP
        /// </summary>
        ALL = SELECT_CLAUSE | WHERE_CLAUSE | ORDERBY_CLAUSE | TOP_CLAUSE
    }

    /// <summary>
    /// ODataToSqlTranslator
    /// </summary>
    public class ODataToSqlTranslator
    {
        /// <summary>
        /// function that takes in an <see cref="ODataQueryOptions"/>, a string representing the type to filter, and a <see cref="FeedOptions"/>
        /// </summary>
        /// <param name="odataQueryOptions"></param>
        /// <param name="translateOptions"></param>
        /// <param name="additionalWhereClause"></param>
        /// <returns>returns an SQL expression if successfully translated, otherwise a null string</returns>
        public string Translate(ODataQueryOptions odataQueryOptions, TranslateOptions translateOptions, string additionalWhereClause = null)
        {
            try
            {
                string selectClause, whereClause, orderbyClause, topClause;
                selectClause = whereClause = orderbyClause = topClause = string.Empty;

                // SELECT CLAUSE
                if ((translateOptions & TranslateOptions.SELECT_CLAUSE) == TranslateOptions.SELECT_CLAUSE)
                {
                    // TOP CLAUSE
                    if ((translateOptions & TranslateOptions.TOP_CLAUSE) == TranslateOptions.TOP_CLAUSE)
                    {
                        topClause = odataQueryOptions?.Top?.Value > 0
                            ? $"{Constants.SQLTopSymbol} {odataQueryOptions.Top.Value} "
                            : string.Empty;
                    }

                    selectClause = odataQueryOptions?.SelectExpand?.RawSelect == null
                        ? "*"
                        : string.Join(", ", odataQueryOptions.SelectExpand.RawSelect.Split(',').Select(c => string.Concat("c.", c.Trim())));
                    selectClause = $"{Constants.SQLSelectSymbol} {topClause}{selectClause} {Constants.SQLFromSymbol} {Constants.SQLFieldNameSymbol} ";
                }

                // WHERE CLAUSE
                if ((translateOptions & TranslateOptions.WHERE_CLAUSE) == TranslateOptions.WHERE_CLAUSE)
                {
                    var customWhereClause = additionalWhereClause == null
                        ? string.Empty
                        : $"{additionalWhereClause}";
                    whereClause = odataQueryOptions?.Filter?.FilterClause == null
                        ? string.Empty
                        : $"{this.TranslateFilterClause(odataQueryOptions.Filter.FilterClause)}";
                    whereClause = (!string.IsNullOrEmpty(customWhereClause) && !string.IsNullOrEmpty(whereClause))
                        ? $"{customWhereClause} AND {whereClause}"
                        : $"{customWhereClause}{whereClause}";
                    whereClause = string.IsNullOrEmpty(whereClause)
                        ? string.Empty
                        : $"{Constants.SQLWhereSymbol} {whereClause} ";
                }

                // ORDER BY CLAUSE
                if ((translateOptions & TranslateOptions.ORDERBY_CLAUSE) == TranslateOptions.ORDERBY_CLAUSE)
                {
                    orderbyClause = odataQueryOptions?.OrderBy?.OrderByClause == null
                        ? string.Empty
                        : $"{Constants.SQLOrderBySymbol} {this.TranslateOrderByClause(odataQueryOptions.OrderBy.OrderByClause)} ";
                }

                return string.Concat(selectClause, whereClause, orderbyClause);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return null;
            }
        }

        public ODataToSqlTranslator(SQLQueryFormatter sqlQueryFormatter)
        {
            oDataNodeToStringBuilder = new ODataNodeToStringBuilder(sqlQueryFormatter);
        }

        private ODataToSqlTranslator() { }

        /// <summary>Translates a <see cref="FilterClause"/> into a <see cref="FilterClause"/>.</summary>
        /// <param name="filterClause">The filter clause to translate.</param>
        /// <returns>The translated string.</returns>
        private string TranslateFilterClause(FilterClause filterClause)
        {
            return oDataNodeToStringBuilder.TranslateNode(filterClause.Expression);
        }

        /// <summary>Translates a <see cref="OrderByClause"/> into a <see cref="OrderByClause"/>.</summary>
        /// <param name="orderByClause">The orderBy clause to translate.</param>
        /// <param name="preExpr">expression built so far.</param>
        /// <returns>The translated string.</returns>
        private string TranslateOrderByClause(OrderByClause orderByClause, string preExpr = null)
        {
            string expr = string.Concat(oDataNodeToStringBuilder.TranslateNode(orderByClause.Expression), Constants.SymbolSpace, orderByClause.Direction == OrderByDirection.Ascending ? Constants.KeywordAscending : Constants.KeywordDescending);

            expr = string.IsNullOrWhiteSpace(preExpr) ? expr : string.Concat(preExpr, Constants.SymbolComma, Constants.SymbolSpace, expr);

            if (orderByClause.ThenBy != null)
            {
                expr = this.TranslateOrderByClause(orderByClause.ThenBy, expr);
            }

            return expr;
        }

        /// <summary>
        /// 
        /// </summary>
        private ODataNodeToStringBuilder oDataNodeToStringBuilder { get; set; }
    }
}