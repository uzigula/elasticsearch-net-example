using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSearch.Domain.Model
{
	public class SuggestField
	{
		public IEnumerable<string> Input { get; set; }
		public string Output { get; set; }
		public object Payload { get; set; }
		public int? Weight { get; set; }
	}
}
