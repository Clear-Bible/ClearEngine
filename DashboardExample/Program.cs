using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation.Thot;

using ClearBible.Engine.Translation;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Utils; //FIXME: use SIL.Util once in current version
using ClearBible.Engine.Persistence;

// set up the parallel corpra
var tokenizer = new LatinWordTokenizer();
// For obtaining target corpra from paratext directly
var targetCorpus = new ParatextTextCorpus(tokenizer, "data/WEB-PT");
// For reading target corpra from usfm file
// var targetCorpus = new UsxFileTextCorpus(tokenizer, "path/to/usx", ScrVers.Original);
var parallelCorpus = new ManuscriptParallelTextCorpus(targetCorpus);

TreeAlignerConfiguration treeAlignerConfiguration = new();
//Select the model used for the smt stage that preceeds tree alignment.
// (Contains a ThotWordAlignmentModel rather than deriving from one of them so different ones can be used.)
using var model = new ManuscriptSyntaxTreeWordAlignmentModel<TreeAlignerConfiguration>(ThotWordAlignmentModelType.FastAlign);
using var trainer = model.CreateManuscriptAlignmentTrainer(parallelCorpus, treeAlignerConfiguration, TokenProcessors.Lowercase);

// Train the model, which first trains the smt algorithm then further refines the aligment results by applying tree alignment.
trainer.Train(new DelegateProgress(status => Console.WriteLine($"Training TreeAligner: {status.PercentCompleted:P}")));

SqlLiteCorporaAlignmentsPersist.Get().SetLocation("connection string").PutAsync(model.CorporaAlignments);
