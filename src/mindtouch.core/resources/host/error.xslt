<?xml version="1.0" encoding="UTF-8" ?>
<html xsl:version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns="http://www.w3.org/1999/xhtml">
  <head>
    <title>
      <xsl:value-of select="error/title"/> (<xsl:value-of select="error/status"/>)
    </title>
  </head>
  <body>
    <h1>
      <xsl:value-of select="error/title"/> (<xsl:value-of select="error/status"/>)
    </h1>
    <br />
    <xsl:value-of select="error/message"/>
  </body>
</html>