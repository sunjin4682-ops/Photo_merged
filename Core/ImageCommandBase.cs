using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photo.Commands;


// -----------------------------------------------------------------------------
// 파일 역할: 필터 명령의 공통 실행/복원 골격을 제공한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        // 실제 필터 명령들이 상속하는 공통 기반 클래스.
        internal abstract class ImageCommandBase : ICommand, IDisposable
        {
            protected readonly Main mainForm;

            private EditorState beforeState;
            private EditorState afterState;
            private bool executedOnce = false;

            /// <summary>
            /// 공통 필터 명령의 생성자.
            /// 어느 Main 폼 상태를 대상으로 작업할지 보관한다.
            /// </summary>
            protected ImageCommandBase(Main mainForm)
            {
                this.mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            }

            /// <summary>
            /// 필터 명령을 실행한다.
            /// 첫 실행 때는 before/after 상태를 캡처하고,
            /// Redo로 다시 실행될 때는 저장해 둔 afterState만 복원한다.
            /// </summary>
            public void Execute()
            {
                if (!executedOnce)
                {
                    // 필터 적용 전의 이미지를 저장한다.
                    beforeState = mainForm.CaptureState();

                    // 실제 픽셀 처리는 파생 클래스가 구현한 ApplyEffect에서 수행한다.
                    ApplyEffect();

                    // 적용이 끝난 결과도 저장해 두어 Redo 시 다시 연산하지 않게 한다.
                    afterState = mainForm.CaptureState();
                    executedOnce = true;
                }
                else
                {
                    // Redo에서는 이미 계산된 결과 상태를 그대로 복원한다.
                    mainForm.ApplyState(afterState);
                }
            }

            /// <summary>
            /// 필터 적용 전 상태로 되돌린다.
            /// Undo는 새로 연산하지 않고 저장해 둔 beforeState를 복원한다.
            /// </summary>
            public void UnExecute()
            {
                if (beforeState != null)
                {
                    mainForm.ApplyState(beforeState);
                }
            }

            protected abstract void ApplyEffect();

            // 사용한 리소스를 해제한다.
            public void Dispose()
            {
                beforeState?.Dispose();
                afterState?.Dispose();
            }
        }
    }
}
