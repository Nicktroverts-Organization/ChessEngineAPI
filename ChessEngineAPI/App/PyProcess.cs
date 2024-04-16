using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineAPI.App
{
    public class PyProcess
    {
        private Process process;
        private string UniqueID;
        private string LastEngineMove = "";

        public Process GetProcess
        {
            get => process;
            set => throw new AccessViolationException("can't be set!");
        }

        public string GetUniqueID
        {
            get => UniqueID;
            set => throw new AccessViolationException("can't be set!");
        }

        public string GetLastEngineMove
        {
            get => LastEngineMove;
            set => throw new AccessViolationException("can't be set!");
        }

        public PyProcess(Process _process, string uniqueID)
        {
            process = _process;
            UniqueID = uniqueID;
        }

        public void MakeMove(string Move)
        {
            process.StandardInput.WriteLine(Move);

            StreamReader reader = new StreamReader(process.StandardOutput.BaseStream);
            LastEngineMove = reader.ReadLine().ToString();
        }
    }
}
