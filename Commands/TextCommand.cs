using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: TextCommand 기능을 명령 패턴으로 실행하고 Undo/Redo와 연결한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {

        /// <summary>
        /// 하나의 텍스트 항목을 비트맵에 실제로 그린 새 이미지를 만든다.
        /// 저장 전 오버레이 상태였던 텍스트를 픽셀로 굳히는 핵심 유틸리티이다.
        /// </summary>
        private Bitmap DrawTextOnBitmap(
       Bitmap source,
       string text,
       Point position,
       string fontName,
       float fontSize,
       FontStyle fontStyle,
       Color textColor)
        {
            Bitmap result = new Bitmap(source);

            using (Graphics g = Graphics.FromImage(result))
            {
                // 화면 미리보기와 비슷한 품질로 텍스트를 비트맵에 렌더링하기 위한 그래픽 옵션들이다.
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                using Font font = new Font(fontName, fontSize, fontStyle);
                using Brush brush = new SolidBrush(textColor);
                g.DrawString(text, font, brush, position);
            }

            return result;
        }

        /// <summary>
        /// 현재 대기 중인 모든 텍스트를 순서대로 비트맵에 렌더링한다.
        /// 저장 버튼 시점에 호출되어 최종 결과 이미지를 생성한다.
        /// </summary>
        internal Bitmap RenderPendingTextsToBitmap(Bitmap source)
        {
            Bitmap current = new Bitmap(source);

            // 텍스트는 리스트 순서대로 그려져 나중 항목이 위에 보이게 된다.
            foreach (TextItem item in textList)
            {
                using Bitmap next = DrawTextOnBitmap(
                    current,
                    item.Text,
                    item.Position,
                    item.SelectedFont,
                    item.FontSize,
                    FontStyle.Bold,
                    item.TextColor);

                current.Dispose();
                current = new Bitmap(next);
            }

            return current;
        }

        /// <summary>
        /// 새 텍스트 항목을 대기 목록에 추가하는 명령.
        /// 텍스트 자체는 아직 비트맵에 굳히지 않고 오버레이 상태로 유지된다.
        /// </summary>
        internal sealed class TextCommand : ICommand
        {
            private readonly Main mainForm;
            private readonly TextItem item;

            // 이 파일의 핵심 동작을 수행하는 메서드.
            public TextCommand(Main form, TextItem textItem)
            {
                mainForm = form;
                item = textItem.Clone();
            }

            // 명령 또는 작업을 실행한다.
            public void Execute()
            {
                mainForm.AddPendingText(item);
            }

            // 직전에 실행한 작업을 되돌린다.
            public void UnExecute()
            {
                mainForm.RemovePendingText(item);
            }
        }

        /// <summary>
        /// 이미 존재하는 텍스트 오버레이의 위치 이동을 기록하는 명령.
        /// 텍스트 드래그도 Undo/Redo 가능하게 만들기 위해 사용한다.
        /// </summary>
        internal sealed class MoveTextCommand : ICommand
        {
            private readonly Main mainForm;
            private readonly TextItem item;
            private readonly Point oldPosition;
            private readonly Point newPosition;

            // 선택된 요소의 위치를 변경한다.
            public MoveTextCommand(Main form, TextItem item, Point oldPosition, Point newPosition)
            {
                mainForm = form;
                this.item = item;
                this.oldPosition = oldPosition;
                this.newPosition = newPosition;
            }

            // 명령 또는 작업을 실행한다.
            public void Execute()
            {
                mainForm.MovePendingText(item, newPosition);
            }

            // 직전에 실행한 작업을 되돌린다.
            public void UnExecute()
            {
                mainForm.MovePendingText(item, oldPosition);
            }
        }
    }
}
