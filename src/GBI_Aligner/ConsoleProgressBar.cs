using System;
using System.IO;
using System.Text;
using System.Threading;
using Models;

namespace GBI_Aligner
{
	public class ConsoleProgressBar : IDisposable, IProgress<Progress>
	{
		public ConsoleProgressBar(TextWriter outWriter)
		{
			m_outWriter = outWriter;
			m_timer = new Timer(TimerHandler, null, Timeout.Infinite, Timeout.Infinite);
			ResetTimer();
		}

		public void Report(Progress progress)
		{
            double ratio;

			// Make sure value is in [0..1] range
			ratio = Math.Max(0, Math.Min(1, progress.IterationRatio));
            Interlocked.Exchange(ref m_currentPhase, progress.Phase);
            Interlocked.Exchange(ref m_currentProgress, ratio);
            Interlocked.Exchange(ref m_currentDelta, progress.MaxDelta);
        }

        private void TimerHandler(object state)
		{
			lock (m_timer)
			{
				if (m_disposed)
					return;

				int progressBlockCount = (int) (m_currentProgress * BlockCount);
				int percent = (int) (m_currentProgress * 100);
				string text = string.Format("[{0}{1}] {2} MaxDelta={3} {4,3}%  {5}",
					new string('#', progressBlockCount), new string('-', BlockCount - progressBlockCount),
					m_currentPhase,
                    m_currentDelta,
                    percent,
                    Animation[m_animationIndex++ % Animation.Length]);
				UpdateText(text);

				ResetTimer();
			}
		}

		private void UpdateText(string text)
		{
			// Get length of common portion
			int commonPrefixLength = 0;
			int commonLength = Math.Min(m_currentText.Length, text.Length);
			while (commonPrefixLength < commonLength && text[commonPrefixLength] == m_currentText[commonPrefixLength])
				commonPrefixLength++;

			// Backtrack to the first differing character
			StringBuilder outputBuilder = new StringBuilder();
			outputBuilder.Append('\b', m_currentText.Length - commonPrefixLength);

			// Output new suffix
			outputBuilder.Append(text.Substring(commonPrefixLength));

			// If the new text is shorter than the old one: delete overlapping characters
			int overlapCount = m_currentText.Length - text.Length;
			if (overlapCount > 0)
			{
				outputBuilder.Append(' ', overlapCount);
				outputBuilder.Append('\b', overlapCount);
			}

			m_outWriter.Write(outputBuilder);
			m_currentText = text;
		}

		private void ResetTimer()
		{
			m_timer.Change(AnimationInterval, TimeSpan.FromMilliseconds(-1));
		}

		public void Dispose()
		{
			lock (m_timer)
			{
				m_disposed = true;
				UpdateText(string.Empty);
			}
		}

        private const int BlockCount = 10;
        private static readonly TimeSpan AnimationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string Animation = @"|/-\";

        private readonly Timer m_timer;
        private readonly TextWriter m_outWriter;

        private string m_currentText = string.Empty;
        private bool m_disposed;
        private int m_animationIndex;

        private string m_currentPhase;
        private double m_currentProgress;
        private double m_currentDelta;
    }
}
