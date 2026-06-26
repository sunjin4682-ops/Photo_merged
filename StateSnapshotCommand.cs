using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo
{
    public partial class Main {
        internal sealed class StateSnapshotCommand : ICommand, IDisposable
        {
            private readonly Main _mainForm;
            private readonly Main.EditorState _beforeState;
            private readonly Main.EditorState _afterState;

            public StateSnapshotCommand(Main mainForm, Main.EditorState beforeState, Main.EditorState afterState)
            {
                _mainForm = mainForm;
                _beforeState = beforeState;
                _afterState = afterState;
            }

            public void Execute()
            {
                _mainForm.ApplyState(_afterState);
            }

            public void UnExecute()
            {
                _mainForm.ApplyState(_beforeState);
            }

            public void Dispose()
            {
                _beforeState?.Dispose();
                _afterState?.Dispose();
            }
        }
    }
    
}
