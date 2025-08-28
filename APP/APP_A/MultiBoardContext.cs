using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace APP_A
{
    /// <summary>
    /// 複数フォームを同時起動して、すべて閉じられたらアプリを終了させるための ApplicationContext。
    /// 既存フォームのロジックには影響しません（新規追加のみ）。
    /// </summary>
    public sealed class MultiBoardContext : ApplicationContext
    {
        private int _openForms;

        public MultiBoardContext(IEnumerable<Form> forms)
        {
            foreach (var f in forms)
            {
                _openForms++;
                f.FormClosed += (_, __) =>
                {
                    if (--_openForms == 0) ExitThread();
                };
                f.Show();
            }
        }
    }
}
