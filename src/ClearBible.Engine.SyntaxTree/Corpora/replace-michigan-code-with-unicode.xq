for $node in //Node[@Unicode]
return
   replace value of node $node with $node/@Unicode
