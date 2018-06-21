namespace Shrike.Data.Reports.Base
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public sealed class ReportRole 
    {
        public String Name { get; set; }
        public int Index { get; set; }

        public override bool Equals(object obj)
        {
            ReportRole role = obj as ReportRole;
            if(role!=null)
            {
                return this.Name.Equals(role.Name) && this.Index == role.Index;
            }
            
            return false;
        }
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }

    public class ReportRoles 
    {
        public static ReportRole TenantOwner { get { return new ReportRole() { Name= "TenantOwner", Index=1 }; } }
        public static ReportRole OperationalTenantOwner { get { return new ReportRole() { Name = "OperationalTenantOwner", Index = 2 }; } }
        public static ReportRole ContentManager { get { return new ReportRole() { Name = "ContentManager", Index = 4 }; } }
        public static ReportRole ContentCreator { get { return new ReportRole() { Name = "ContentCreator", Index = 8 }; } }
        public static ReportRole Analyst { get { return new ReportRole() { Name = "Analyst", Index = 16 }; } }
    }

}
