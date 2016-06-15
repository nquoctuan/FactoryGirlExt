using System.Collections.Generic;
using System.Linq;

namespace FactoryGirlExt.Helpers
{
    public class PropertyContainer
    {
        private readonly Dictionary<string, object> _Ids;
        private readonly Dictionary<string, object> _Values;

        #region Constructor

        internal PropertyContainer()
        {
            _Ids = new Dictionary<string, object>();
            _Values = new Dictionary<string, object>();
        }

        #endregion

        #region Properties

        internal IEnumerable<string> IdNames
        {
            get { return _Ids.Keys; }
        }

        internal IEnumerable<string> ValueNames
        {
            get { return _Values.Keys; }
        }

        internal IEnumerable<string> AllNames
        {
            get { return _Ids.Keys.Union(_Values.Keys); }
        }

        internal IDictionary<string, object> IdPairs
        {
            get { return _Ids; }
        }

        internal IDictionary<string, object> ValuePairs
        {
            get { return _Values; }
        }

        internal IEnumerable<KeyValuePair<string, object>> AllPairs
        {
            get { return _Ids.Concat(_Values); }
        }

        #endregion

        #region Methods

        internal void AddId(string name, object value)
        {
            _Ids.Add(name, value);
        }

        internal void AddValue(string name, object value)
        {
            _Values.Add(name, value);
        }

        #endregion
    }
}