using System.Text;
using System.Xml.Linq;

using ClearBible.Engine.Tokenization;

using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace ClearBible.Engine.Corpora
{
	/// <summary>
	/// Used to obtain the Paratext Texts, with each Text corresponding to a book identified (Id) 
	/// by its three character book designation, e.g. "1JN'.
	/// 
	/// Segments, each of which is a verse id and text, can then be obtained by Text.GetSegments().
	/// 
	/// This override returns custom Engine texts through a new method GetEngineText() which doesn't attempt to 
	/// group segments by versification when Engine wants to replace Machine's versification with its own versification mapping.
	/// </summary>
	public class EngineParatextTextCorpus : ParatextTextCorpus, IEngineCorpus
	{
        public EngineParatextTextCorpus(
			ITokenizer<string, int, string> wordTokenizer, 
			string projectDir, 
			ITextSegmentProcessor? textSegmentProcessor = null,
			bool includeMarkers = false)
            : base(wordTokenizer, projectDir, includeMarkers)
        {
			TextSegmentProcessor = textSegmentProcessor;

			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			string? settingsFileName = Path.Combine(projectDir, "Settings.xml");
			if (!File.Exists(settingsFileName))
				settingsFileName = Directory.EnumerateFiles(projectDir, "*.ssf").FirstOrDefault();
			if (string.IsNullOrEmpty(settingsFileName))
			{
				throw new ArgumentException("The project directory does not contain a settings file.",
					nameof(projectDir));
			}
			var settingsDoc = XDocument.Load(settingsFileName);
			var encodingStr = (string?)settingsDoc.Root?.Element("Encoding") ?? "65001";
			if (!int.TryParse(encodingStr, out int codePage))
			{
				throw new NotImplementedException(
					$"The project uses a legacy encoding that requires TECKit, map file: {encodingStr}.");
			}
			var encoding = Encoding.GetEncoding(codePage);

			var stylesheetName = (string?)settingsDoc.Root?.Element("StyleSheet") ?? "usfm.sty";
			string stylesheetFileName = Path.Combine(projectDir, stylesheetName);
			if (!File.Exists(stylesheetFileName) && stylesheetName != "usfm_sb.sty")
				stylesheetFileName = Path.Combine(projectDir, "usfm.sty");
			string customStylesheetPath = Path.Combine(projectDir, "custom.sty");
			var stylesheet = new UsfmStylesheet(stylesheetFileName,
				File.Exists(customStylesheetPath) ? customStylesheetPath : null);

			string prefix = "";
			string suffix = ".SFM";
			XElement? namingElem = settingsDoc.Root?.Element("Naming");
			if (namingElem != null)
			{
				var prePart = (string?)namingElem?.Attribute("PrePart");
				if (!string.IsNullOrEmpty(prePart))
					prefix = prePart;
				var postPart = (string?)namingElem?.Attribute("PostPart");
				if (!string.IsNullOrEmpty(postPart))
					suffix = postPart;
			}

			TextDictionary.Clear();
			foreach (string sfmFileName in Directory.EnumerateFiles(projectDir, $"{prefix}*{suffix}"))
			{
				AddText(new EngineUsfmFileText(wordTokenizer, stylesheet, encoding, sfmFileName, Versification,
					includeMarkers, this));
			}
		}

		public ITextSegmentProcessor? TextSegmentProcessor { get; set; }
		public bool DoMachineVersification { get; set; } = true;
    }
}
