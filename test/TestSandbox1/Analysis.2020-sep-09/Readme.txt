
This folder contains artifacts related to comparison of the behavior after porting to the original behavior, as further described below.

Slack Messages to Andi Wu and Charles Lee in #clear-engine channel
2020-sep-09

Tim Sauerwein  5:13 PM
@Andi @Charles Lee I have ported the Clear code from .NET Framework to .Net Standard, and I am running the result on Mac and comparing what it produces to the original running on Windows.  My test case is a New Testament example that Andi shared with me some time ago.
When I run the machine learning step, I get the same translation model and alignment model in both cases, and the floating point numbers that occur in these models are the same if I stick with the first 8 significant digits.  However, the floating point numbers do not exactly match in all cases.
These discrepancies might be due to slight changes in the calculations, because I changed the underlying system libraries and the operating system.  Or there could be some other difference that has escaped my notice.
When I go on to run the auto-alignment, I get a slightly different answer.  (I’m still looking into the nature of this difference.)  The auto-alignment difference might be because auto-alignment is a sensitive function of the floating point numbers that occur in the translation and alignment models; does that idea seem plausible to you?
Have you noticed any behaviors in the past that are similar to what I am describing?

Tim Sauerwein  5:18 PM
(I also found that the respective translation models were written to their output files in different orders, which I put down to the fact that the underlying hash tables ended up having different internal representations.)