using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeployLX.CodeVeil.CompileTime.v5;

namespace Zapuskator.ViewModels
{
    
    public class HelpViewModel
    {
        public int SelectedTab { get; set; }
        public HelpViewModel(int index=0)
        {
            SelectedTab = index;
        }
    }
}
