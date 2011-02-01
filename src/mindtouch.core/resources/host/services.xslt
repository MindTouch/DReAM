<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html"  encoding="UTF-8" indent="no" />

  <xsl:template match="services">
	  <html>
			<head>
				<title>MindTouch Dream Running Services</title>
			</head>
			<body>
        <h1>Running Services</h1>
        <ul>
        <xsl:apply-templates select="service" />
        </ul>
      </body>
		</html>
	</xsl:template>
	
	<xsl:template match="service">
    <li>
      <a href="{uri}/@about">
        <xsl:value-of select="path"/>
      </a>
      <ul>
        <li>SID: 
          <xsl:value-of select="sid"/>
        </li>
        <li>
          Type: <xsl:value-of select="type"/>
        </li>
      </ul>
    </li>
  </xsl:template>
</xsl:stylesheet>
