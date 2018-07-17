/* GraphQLQuery.cs
 * This class specifies the GraphQL Query object.
 * 
 * Copyright (c) 2018 MAL Updater OS X Group, a division of Moy IT Solutions
 * Licensed under MIT License
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nekomata
{
    public class GraphQLQuery
    {
        public string query { get; set; }
        public object variables { get; set; }
    }
}
