<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html"  encoding="UTF-8" indent="no" />

  <xsl:template match="status">
	  <html>
			<head>
				<title>MindTouch Dream Status</title>
			</head>
			<body>
        <xsl:apply-templates select="host" />
        <xsl:apply-templates select="system" />
      </body>
		</html>
	</xsl:template>
	
	<xsl:template match="host">
		<h1>Host Status</h1>
		<p>
      <ul>
        <li>Running since <xsl:value-of select="@created" /> (<xsl:call-template name="calc_age" />)</li>
        <li>Processed <xsl:value-of select="@requests" /> requests (<xsl:value-of select="@rate" /> requests/sec)</li>
        <li>Cache <xsl:value-of select="cache/@hits" /> hits, <xsl:value-of select="cache/@misses" /> misses (ratio <xsl:value-of select="number(cache/@hits) div (number(cache/@hits)+number(cache/@misses)) * 100" />%, <xsl:value-of select="cache/@count" /> messages, <xsl:value-of select="number(cache/@size) div 1048576" /> MB)</li>
        <li><xsl:value-of select="connections/@count" /> connections (max <xsl:value-of select="connections/@limit" />)</li>
			  <li><a href="{services/@href}"><xsl:value-of select="services/@count" /> services</a></li>
			  <li><a href="{aliases/@href}"><xsl:value-of select="aliases/@count" /> aliases</a></li>
			  <li><a href="{features/@href}">feature list</a></li>
      </ul>
		</p>
		<xsl:apply-templates select="activities" />
    <xsl:apply-templates select="infos" />
  </xsl:template>
  
  <xsl:template match="system">
    <h2>System Status</h2>
    <p><ul>
			<li><xsl:value-of select="number(memory.used) div 1048576" /> MB managed memory</li>
			<li><xsl:value-of select="workerthreads.used" /> worker threads (max <xsl:value-of select="workerthreads.max" />)</li>
			<li><xsl:value-of select="completionthreads.used" /> completion threads (max <xsl:value-of select="completionthreads.max" />)</li>
			<li><xsl:value-of select="dispatcherthreads.used" /> dispatcher threads (max <xsl:value-of select="dispatcherthreads.max" />)</li>
			<li><a href="{timers.queued/@href}"><xsl:value-of select="timers.queued" /> timers queued,  <xsl:value-of select="timers.pending" /> pending</a></li>
			<li>Last timer run <xsl:value-of select="timers.last" />, next maintenance <xsl:value-of select="timers.next" /> (<xsl:value-of select="timers.counter" /> iterations)</li>
			<li><xsl:value-of select="async/@count" /> active asynchronous operations</li>
			<xsl:if test="xmlnametable">
				<li><a href="{xmlnametable/@href}">Global XmlNameTable:</a><ul>
					<li>capacity: <xsl:value-of select="xmlnametable/capacity" /></li>
					<li>entries: <xsl:value-of select="xmlnametable/entries" /></li>
					<li>size: <xsl:value-of select="xmlnametable/bytes" /> bytes</li>
					<li>expected comparisons: <xsl:value-of select="xmlnametable/expected-comparisons" /> per lookup</li>
					<li>distribution: <xsl:value-of select="xmlnametable/distribution" /></li>
				</ul>
			</li>
			</xsl:if>
			<li>App.config settings:<ul>
				<xsl:for-each select="app-settings/entry">
					<li><xsl:value-of select="@key" /> = <xsl:value-of select="@value" /></li>
				</xsl:for-each>
			</ul>
			</li>
		</ul></p>
  </xsl:template>
	
	<xsl:template match="activities">
		<h2>Activities (<xsl:value-of select="@count" />)</h2>
		<ul>
			<xsl:for-each select="description"><li><xsl:value-of select="text()" /><br />Running since <xsl:value-of select="@created" /> (<xsl:call-template name="calc_age" />)</li></xsl:for-each>
		</ul>
	</xsl:template>

	<xsl:template match="infos">
		<h2>Info Sources (<xsl:value-of select="@count" />)</h2>
		<ul><xsl:for-each select="info"><li><xsl:value-of select="@source" /> =&gt; <xsl:value-of select="@hits" /> hits (<xsl:value-of select="@rate" /> hits/sec)</li></xsl:for-each></ul>
	</xsl:template>

	<xsl:template name="calc_age">
    <xsl:choose>
		  <xsl:when test="number(@age) &lt; 60"><xsl:value-of select="@age" /> seconds</xsl:when>
		  <xsl:otherwise><xsl:value-of select="floor(number(@age) div 86400)" /> days, <xsl:value-of select="floor(number(@age) mod 86400 div 3600)" /> hours, <xsl:value-of select="floor(number(@age) mod 3600 div 60)" /> minutes, <xsl:value-of select="floor(number(@age) mod 60)" /> seconds</xsl:otherwise>
	  </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
