# Collection examples

These fictional collections are tracked test fixtures. Open any CSV through the
application's **Open file** action. Keep its matching `.metadata.json` file beside
it so the application loads the correct collection type, delimiter, and statistics.
TV series use the application's `Series` collection type.

Each collection has two examples:

| Collection | Basic example (4 records) | Edge-case example (6 records) | Edge-case delimiter |
| --- | --- | --- | --- |
| Books | [books-basic.csv](books-basic.csv) | [books-edge-cases.csv](books-edge-cases.csv) | Tab |
| Comics | [comics-basic.csv](comics-basic.csv) | [comics-edge-cases.csv](comics-edge-cases.csv) | Semicolon |
| Films | [films-basic.csv](films-basic.csv) | [films-edge-cases.csv](films-edge-cases.csv) | Tab |
| Games | [games-basic.csv](games-basic.csv) | [games-edge-cases.csv](games-edge-cases.csv) | Semicolon |
| TV series | [tv-series-basic.csv](tv-series-basic.csv) | [tv-series-edge-cases.csv](tv-series-edge-cases.csv) | Pipe |

All basic examples use commas. Files are UTF-8, use the application's default
column headers exactly, and omit the generated `#` row-number column. Dates use
`dd/MM/yyyy`; ratings use a period for decimals. Values are illustrative, not
factual recommendations or personal collection data.

The edge-case files include Unicode, embedded delimiters, escaped double quotes,
multiline comments/reviews, long text, repeated release years, and empty optional
fields. Quoted multiline values mean that physical line counts exceed record
counts. The games and series examples also exercise different completion states.

The JSON companions enable statistics on open and leave automatic refresh off.
Basic examples contain total-record and average-rating queries. Edge-case
examples also group by release year and include three recently deleted statistics
for testing the restore popup. Their deletion dates are fixed at 5 November,
4 November, and 31 October 2026; displayed relative ages change over time.

For a fresh load, expect 4 records and an average rating of 4.25 (basic games: 4.0;
basic TV series: 4.5), or 6 records and an average rating of 4.13 for the edge-case
files. Edge-case
year counts are 2 for 2018, 3 for 2022, and 1 blank year. Restoring the sample
deleted queries should report 4 rated records, 5 records with comments, and a
highest rating of 5.

Use **Save as** to a local working directory when experimenting if you want to
keep these fixtures unchanged. Saving in place intentionally appears as a Git
change, including edits to companion statistics and refresh preferences.
