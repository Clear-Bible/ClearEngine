
2020-sep-10

This folder holds collateral for a comparison between the translation models
that were computed by three different runs of the program with the same input
data.

Slack conversation with Andi and Charles on 2020-sep-10:

Tim Sauerwein Today at 11:26 AM
@Andi I ran the (ported version of) Clear three times with the same inputs each time.  The input was the New Testament example that you shared with me before.  Each run produced the same outputs, except:
the order of the lines in transModel.txt was different each time (which I put down to the randomness in the Hashtables, as you say), and
the final auto alignments were slightly different each time, with variations in the numbers of links in about 50 of the verses.
Is it plausible that the auto-alignment should be a sensitive function of the order in which the translation model is stored in the hash table?


Charles Lee  5 hours ago
Yes. As I responded to an earlier message of yours, currently, I found one place where the order of candidates in a Hashtable DOES make a difference in the eventual alignments.
Take a look at the ComputeTopCandidates() in Align.cs. Specifically look at CreatePaths() and FilterPaths(). The "one place" I mentioned happens at FilterPaths().
I commented this part of the code (including the if statement afterwards, and just made paths the same as allPaths), and it solved the difference in alignment for the one verse I debugged, but then there were many more other differences that appeared (and probably disappeared as well), so it really didn't completely solve my problem of why running the same code from Forms versus the command line would produce different alignments. This is why I suspect there may be other places where the same issue of order in a Hashtable pops it's ugly head.

Tim Sauerwein  4 hours ago
Thanks Charles.

Andi  2 hours ago
Yes, Charles and I discussed this before.  Given the current code, slight variation in the output is expected.