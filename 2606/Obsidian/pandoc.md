If you need to convert files from one markup format into another, pandoc is your swiss-army knife. Pandoc can convert between the following formats:

(← = conversion from; → = conversion to; ↔︎ = conversion from and to)

Lightweight markup formats

↔︎ [Markdown](https://daringfireball.net/projects/markdown/) (including [CommonMark](https://commonmark.org/) and [GitHub-flavored Markdown](https://github.github.com/gfm/))  
↔︎ [reStructuredText](https://docutils.sourceforge.net/docs/ref/rst/introduction.html)  
↔︎ [AsciiDoc](https://asciidoc.org/)  
↔︎ Emacs [Org-Mode](https://orgmode.org/)  
↔︎ Emacs [Muse](https://www.gnu.org/software/emacs-muse/manual/)  
↔︎ [Textile](https://textile-lang.com/)  
→ [Markua](https://leanpub.com/markua/read)  [[]]
← [txt2tags](https://txt2tags.org/)  
↔︎ [djot](https://djot.net/)  
→ [BBCode](https://www.bbcode.org/reference.php)

HTML formats

↔︎ (X)HTML 4  
↔︎ HTML5  
→ Chunked HTML

Ebooks

↔︎ [EPUB](https://en.wikipedia.org/wiki/EPUB) version 2 or 3  
↔︎ [FictionBook2](http://www.fictionbook.org/index.php/Eng:XML_Schema_Fictionbook_2.1)

Documentation formats

→ [GNU TexInfo](https://www.gnu.org/software/texinfo/)  
← [pod](https://perldoc.perl.org/perlpod)  
↔︎ [Haddock markup](https://www.haskell.org/haddock/doc/html/ch03s08.html)  
→ [Vimdoc](https://vimhelp.org/helphelp.txt.html#help-writing)

Roff formats

↔︎ [roff man](https://www.gnu.org/software/groff/groff.html)  
→ [roff ms](https://www.gnu.org/software/groff/groff.html)  
← [mdoc](https://mandoc.bsd.lv/man/mdoc.7.html)

TeX formats

↔︎ [LaTeX](https://www.latex-project.org/)  
→ [ConTeXt](https://www.pragma-ade.nl/)

XML formats

↔︎ [DocBook](https://www.docbook.org/) version 4 or 5  
↔︎ [JATS](https://jats.nlm.nih.gov/publishing/)  
← [BITS](https://jats.nlm.nih.gov/extensions/bits/)  
→ [TEI Simple](https://github.com/TEIC/TEI-Simple)  
→ [OpenDocument XML](https://opendocument.xml.org/)

Outline formats

↔︎ [OPML](http://dev.opml.org/spec2.html)

Bibliography formats

↔︎ [BibTeX](https://tug.org/bibtex/)  
↔︎ [BibLaTeX](https://github.com/plk/biblatex)  
↔︎ [CSL JSON](https://citeproc-js.readthedocs.io/en/latest/csl-json/markup.html)  
↔︎ CSL YAML  
← [RIS](https://en.wikipedia.org/wiki/RIS_\(file_format\))  
← [EndNote XML](https://support.clarivate.com/Endnote/s/article/EndNote-XML-Document-Type-Definition)

Word processor formats

↔︎ Microsoft Word [docx](https://en.wikipedia.org/wiki/Office_Open_XML)  
↔︎ Rich Text Format [RTF](https://en.wikipedia.org/wiki/Rich_Text_Format)  
↔︎ OpenOffice/LibreOffice [ODT](https://en.wikipedia.org/wiki/OpenDocument)  
↔︎ OpenOffice/LibreOffice [fodt]

Interactive notebook formats

↔︎ Jupyter notebook ([ipynb](https://nbformat.readthedocs.io/en/latest/))

Page layout formats

→ [InDesign ICML](https://web.archive.org/web/20211006210211/https://wwwimages.adobe.com/www.adobe.com/content/dam/acom/en/devnet/indesign/sdk/cs6/idml/idml-cookbook.pdf)  
↔︎ [Typst](https://typst.app/)

Wiki markup formats

↔︎ [MediaWiki markup](https://www.mediawiki.org/wiki/Help:Formatting)  
↔︎ [DokuWiki markup](https://www.dokuwiki.org/wiki:syntax)  
← [TikiWiki markup](https://doc.tiki.org/Wiki-Syntax-Text#The_Markup_Language_Wiki-Syntax)  
← [TWiki markup](https://twiki.org/cgi-bin/view/TWiki/TextFormattingRules)  
← [Vimwiki markup](https://vimwiki.github.io/)  
→ [XWiki markup](https://www.xwiki.org/xwiki/bin/view/Documentation/UserGuide/Features/XWikiSyntax/)  
→ [ZimWiki markup](https://zim-wiki.org/manual/Help/Wiki_Syntax.html)  
↔︎ [Jira wiki markup](https://jira.atlassian.com/secure/WikiRendererHelpAction.jspa?section=all)  
← [Creole](http://www.wikicreole.org/)

Slide show formats

→ [LaTeX Beamer](https://ctan.org/pkg/beamer)  
↔︎ Microsoft [PowerPoint](https://en.wikipedia.org/wiki/Microsoft_PowerPoint)  
→ [Slidy](https://www.w3.org/Talks/Tools/Slidy)  
→ [reveal.js](https://lab.hakim.se/reveal-js/)  
→ [Slideous](https://goessner.net/articles/slideous/)  
→ [S5](https://meyerweb.com/eric/tools/s5/)  
→ [DZSlides](https://paulrouget.com/dzslides/)

Data formats

← [CSV](https://tools.ietf.org/html/rfc4180) tables  
← [TSV](https://www.iana.org/assignments/media-types/text/tab-separated-values) tables  
← Microsoft [Excel](https://en.wikipedia.org/wiki/Microsoft_Excel) spreadsheets

Terminal output

→ [ANSI](https://en.wikipedia.org/wiki/ANSI_escape_code)-formatted text

Serialization formats

↔︎ Haskell AST  
↔︎ JSON representation of AST  
↔︎ XML representation of AST

Custom formats

↔︎ [custom readers](https://pandoc.org/custom-readers.html) and [writers](https://pandoc.org/custom-writers.html) can be written in [Lua](https://www.lua.org/)

PDF

→ via `pdflatex`, `lualatex`, `xelatex`, `latexmk`, `tectonic`, `wkhtmltopdf`, `weasyprint`, `prince`, `pagedjs-cli`, `context`, or `pdfroff`.

Pandoc understands a number of useful markdown syntax extensions, including document metadata (title, author, date); footnotes; tables; definition lists; superscript and subscript; strikeout; enhanced ordered lists (start number and numbering style are significant); running example lists; delimited code blocks with syntax highlighting; smart quotes, dashes, and ellipses; markdown inside HTML blocks; and inline LaTeX. If strict markdown compatibility is desired, all of these extensions can be turned off.

LaTeX math (and even macros) can be used in markdown documents. Several different methods of rendering math in HTML are provided, including MathJax and translation to MathML. LaTeX math is converted (as needed by the output format) to unicode, native Word equation objects, MathML, or roff eqn.

Pandoc includes a powerful system for automatic citations and bibliographies. This means that you can write a citation like

```
[see @doe99, pp. 33-35; also @smith04, ch. 1]
```

and pandoc will convert it into a properly formatted citation using any of hundreds of [CSL](https://citationstyles.org/) styles (including footnote styles, numerical styles, and author-date styles), and add a properly formatted bibliography at the end of the document. The bibliographic data may be in [BibTeX](https://tug.org/bibtex/), [BibLaTeX](https://github.com/plk/biblatex), [CSL JSON](https://citeproc-js.readthedocs.io/en/latest/csl-json/markup.html), or CSL YAML format. Citations work in every output format.

There are many ways to customize pandoc to fit your needs, including a template system and a powerful system for writing filters.

Pandoc includes a Haskell library and a standalone command-line program. The library includes separate modules for each input and output format, so adding a new input or output format just requires adding a new module.

Pandoc is free software, released under the [GPL](https://www.gnu.org/copyleft/gpl.html). Copyright 2006–2025 [John MacFarlane](https://johnmacfarlane.net/).

[Code signing policy](https://pandoc.org/code-signing-policy.html)