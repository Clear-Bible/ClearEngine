using System.Collections.Generic;
using System.Linq;
using Xunit;

using ClearBible.Engine.Utils;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;
using System.Reflection.Emit;

namespace ClearBible.Engine.Tests.Corpora
{
	public class EntityIdTests
    {
		public EntityIdTests()
		{
		}

        [Fact]
		public void EntityId__CheckEqualityForTypeAndId()
		{
			var testId = new TestId("11");
			var testId2 = new TestId("12");

			var test2Id = new Test2Id("21");
			test2Id.Id = testId.Id;

			var (fullyQualifiedName, guid) = testId.GetNameAndId();
			 
			var idTokenId = fullyQualifiedName.CreateInstanceByNameAndSetId(guid);
			Assert.IsType<EntityId<TestId>>(idTokenId);

			Assert.True(testId.IdEquals(idTokenId));
			Assert.False(test2Id.IdEquals(idTokenId));
            Assert.False(testId2.IdEquals(idTokenId));
        }

        [Fact]
        public void EntityId__CombiningWithIdEquatablesCollections()
        {
            var testId = new TestId("11");
            var (fullyQualifiedNameTestId, guidTestId) = testId.GetNameAndId();
            var idTestId = fullyQualifiedNameTestId.CreateInstanceByNameAndSetId(guidTestId);
            Assert.IsType<EntityId<TestId>>(idTestId);

            var testId2 = new TestId("12");
            var (fullyQualifiedNameTestId2, guidTestId2) = testId2.GetNameAndId();
            var idTestId2 = fullyQualifiedNameTestId2.CreateInstanceByNameAndSetId(guidTestId2);
            Assert.IsType<EntityId<TestId>>(idTestId2);

            var test2Id = new Test2Id("21");
            var (fullyQualifiedNameTest2Id, guidTest2Id) = test2Id.GetNameAndId();
            var idTest2Id = fullyQualifiedNameTest2Id.CreateInstanceByNameAndSetId(guidTest2Id);
            Assert.IsType<EntityId<Test2Id>>(idTest2Id);

            var test2Id2 = new Test2Id("22");
            var (fullyQualifiedNameTest2Id2, guidTest2Id2) = test2Id2.GetNameAndId();
            var idTest2Id2 = fullyQualifiedNameTest2Id2.CreateInstanceByNameAndSetId(guidTest2Id2);
            Assert.IsType<EntityId<Test2Id>>(idTest2Id2);

            // these are ids of objects in a section of the UI by type
            List<TestId> testIds = new() { testId, testId2 };
            List<Test2Id> test2Ids = new() { test2Id, test2Id2 };

            // these are ids of all the objects in a section of the UI.
            List<IId> ids = new() { testId, testId2, test2Id, test2Id2 };

            List<Note> notes = new()
            {
                new Note("howdy from note 1", new List<IId> { idTestId, idTest2Id, idTest2Id2 }),
                new Note("howdy from note 2", new List<IId> { idTest2Id, idTest2Id2 }),
                new Note("howdy from note 3", new List<IId> { idTestId2, idTest2Id2 })
            };

            var testIdsNotes = testIds.Combine(notes).ToList();
            Assert.True(((Note)testIdsNotes[0].idEquatablesCollections[0]).Text.Equals("howdy from note 1"));
            Assert.Single(testIdsNotes[0].idEquatablesCollections);
            Assert.True(((Note)testIdsNotes[1].idEquatablesCollections[0]).Text.Equals("howdy from note 3"));
            Assert.Single(testIdsNotes[0].idEquatablesCollections);

            var test2IdsNotes = test2Ids.Combine(notes).ToList();
            Assert.True(((Note)test2IdsNotes[0].idEquatablesCollections[0]).Text.Equals("howdy from note 1"));
            Assert.True(((Note)test2IdsNotes[0].idEquatablesCollections[1]).Text.Equals("howdy from note 2"));
            Assert.Equal(2, test2IdsNotes[0].idEquatablesCollections.Count);
            Assert.True(((Note)test2IdsNotes[1].idEquatablesCollections[0]).Text.Equals("howdy from note 1"));
            Assert.True(((Note)test2IdsNotes[1].idEquatablesCollections[1]).Text.Equals("howdy from note 2"));
            Assert.True(((Note)test2IdsNotes[1].idEquatablesCollections[2]).Text.Equals("howdy from note 3"));
            Assert.Equal(3, test2IdsNotes[1].idEquatablesCollections.Count);

            var idsNotes = ids.Combine(notes).ToList();
            Assert.True(((Note)idsNotes[0].idEquatablesCollections[0]).Text.Equals("howdy from note 1"));
            Assert.Single(idsNotes[0].idEquatablesCollections);
            Assert.True(((Note)idsNotes[1].idEquatablesCollections[0]).Text.Equals("howdy from note 3"));
            Assert.Single(idsNotes[1].idEquatablesCollections);
            Assert.True(((Note)idsNotes[2].idEquatablesCollections[0]).Text.Equals("howdy from note 1"));
            Assert.True(((Note)idsNotes[2].idEquatablesCollections[1]).Text.Equals("howdy from note 2"));
            Assert.Equal(2, idsNotes[2].idEquatablesCollections.Count);
            Assert.True(((Note)idsNotes[3].idEquatablesCollections[0]).Text.Equals("howdy from note 1"));
            Assert.True(((Note)idsNotes[3].idEquatablesCollections[1]).Text.Equals("howdy from note 2"));
            Assert.True(((Note)idsNotes[3].idEquatablesCollections[2]).Text.Equals("howdy from note 3"));
            Assert.Equal(3, idsNotes[3].idEquatablesCollections.Count);
        }

        [Fact]
        public void EntityId__CombiningTokenIdAndCompositeTokenIdsWithIdEquatablesCollections()
        {
            //create tokens
            var corpus = new DictionaryTextCorpus(
            new MemoryText("text1", new[]
            {
                        TestHelpers.CreateTextRow(new VerseRef(1,1,1), "Source segment Jacob 1", isSentenceStart: true),
            }))
            .Tokenize<LatinWordTokenizer>()
            .Transform<IntoTokensTextRowProcessor>()
            .ToList(); //so it only tokenizes and transforms once.

            //build new tokens list for first verse that includes a composite token
            var tokens = corpus
                .Select(tr => (TokensTextRow)tr)
                .First()
                .Tokens;

            //create tokens with a composite
            var tokensWithComposite = new List<Token>()
            {
                new CompositeToken(new List<Token>() { tokens[0], tokens[1], tokens[3] }),
                tokens[2]
            };

            //get tokenIds
            var tokensWithCompositeIds = tokensWithComposite
                .Select(t => t.TokenId)
                .ToList();


            //get form of ids that will be put into and taken out of notes assoc table
            var entityIdTokenIds = tokensWithCompositeIds
                .Select(t =>
                {
                    var (fullyQualifiedName, guid) = t.GetNameAndId();
                    return fullyQualifiedName.CreateInstanceByNameAndSetId(guid);
                }).ToList();

            //construct notes with form of ids that will come out of notes assoc table. 
            List<Note> notes = new()
            {
                new Note("howdy from note 1", new List<IId> { entityIdTokenIds[0], entityIdTokenIds[1] }),
                new Note("howdy from note 2", new List<IId> { entityIdTokenIds[0] }),
                new Note("howdy from note 3", new List<IId> { entityIdTokenIds[1]})
            };

            //combine notes with verse tokens that include both a CompositeToken and a Token so UI can associate notes with tokens.
            var tokensWithCompositeIdNotes = tokensWithCompositeIds.Combine(notes).ToList();
            Assert.True(((Note)tokensWithCompositeIdNotes[0].idEquatablesCollections[0]).Text.Equals("howdy from note 1"));
            Assert.True(((Note)tokensWithCompositeIdNotes[0].idEquatablesCollections[1]).Text.Equals("howdy from note 2"));
            Assert.Equal(2, tokensWithCompositeIdNotes[0].idEquatablesCollections.Count);

            Assert.True(((Note)tokensWithCompositeIdNotes[1].idEquatablesCollections[0]).Text.Equals("howdy from note 1"));
            Assert.True(((Note)tokensWithCompositeIdNotes[1].idEquatablesCollections[1]).Text.Equals("howdy from note 3"));
            Assert.Equal(2, tokensWithCompositeIdNotes[1].idEquatablesCollections.Count);
        }
    }

    public class TestId : EntityId<TestId>
	{
		public TestId(string foo)
		{ 
			Foo = foo;
		}
		public string Foo { get; }
	}

    public class Test2Id : EntityId<Test2Id>
    {
        public Test2Id(string bar)
        {
            Bar = bar;
        }
        public string Bar { get; }
    }

    public class Note : IdEquatableCollection
    {
        public Note(string text, IEnumerable<IIdEquatable> idEquatables) : base(idEquatables)
        {
            Text = text;
        }

        public string Text { get; }
    }
}