﻿using MrCMS.Entities.Documents.Metadata;
using MrCMS.Entities.Documents.Web;

namespace MrCMS.Tests.Stubs
{
    public class BasicMappedWebpage : Webpage
    {
    }

    public class BasicMappedNoChildrenInNavWebpage : Webpage
    {
        
    }

    public class BasicMappedNoChildrenInNavWebpageMetadataMap : WebpageMetadataMap<BasicMappedNoChildrenInNavWebpage>
    {
        public override bool ShowChildrenInAdminNav
        {
            get { return false; }
        }   
    }
}