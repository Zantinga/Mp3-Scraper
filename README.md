# Mp3-Scraper
A simple mp3 scraper for downloading audiobooks from manyaudiobooks.net

## To Publish:
`dotnet publish -c Release -r win-x64 --self-contained false`

## To Publish for Windows:
`dotnet publish -c Release -r win-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:EnableCompressionInSingleFile=true
`

## To Run:
`Mp3Scraper.exe https://example.com/page-with-audio`
