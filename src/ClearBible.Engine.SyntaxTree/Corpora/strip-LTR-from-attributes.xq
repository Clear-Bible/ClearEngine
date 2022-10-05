for $unicode in //@*
where contains($unicode,  "&#x200e;")
let $tokens := $unicode ! tokenize(., "&#x200e;") 
return 
  replace value of node $unicode 
  with string-join(reverse($tokens), "")
