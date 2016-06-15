namespace FactoryGirlExt.Helpers
{
    public static class ObjectExtension
    {
        public static string ToSqlQueryCompatible(this object obj)
        {
            if (obj == null)
            {
                return "NULL";
            }

            decimal dummy;
            return decimal.TryParse(obj.ToString(), out dummy) ? obj.ToString() : string.Format("'{0}'", obj);
        }
    }
}