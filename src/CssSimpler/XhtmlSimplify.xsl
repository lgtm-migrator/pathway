<?xml version="1.0" encoding="UTF-8"?>
<!-- #############################################################
    # Name:        XhtmlSimpler.xsl
    # Purpose:     Simplify Xhtml output removing extra hierarchy
    #
    # Author:      Greg Trihus <greg_trihus@sil.org>
    #
    # Created:     2016/4/6
    # Copyright:   (c) 2016 SIL International
    # Licence:     <LPGL>
    ################################################################-->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:x="http://www.w3.org/1999/xhtml"
    version="1.0">

    <xsl:output encoding="UTF-8" method="xml" />

    <xsl:template match="/">
        <xsl:text disable-output-escaping="yes">&lt;!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"[]></xsl:text>
        <xsl:apply-templates select="node()|@*"/>
    </xsl:template>

    <!-- Recursive template -->
    <xsl:template match="node() | @*">
        <xsl:copy>
            <xsl:apply-templates select="node() | @*"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="x:a/@shape"/>
    <xsl:template match="x:head/@profile"/>
    <xsl:template match="x:html/@version"/>
    
    <!-- Matches translation in translation and just puts one level -->
    <xsl:template match="*[@class='translation'][child::*[@class='translation']]">
        <xsl:apply-templates select="*"/>
    </xsl:template>

    <!-- Matches span pictures and puts out its children (which are divs) to correct structure -->
    <xsl:template match="x:span[@class='pictures']">
        <xsl:apply-templates select="*"/>
    </xsl:template>

    <!-- Matches example within example and changes internal name to text (of example) -->
    <xsl:template match="*[@class='example' and child::*/@class='example']">
        <xsl:copy>
            <xsl:attribute name="class">complete</xsl:attribute>
            <xsl:apply-templates select="node()"/>
        </xsl:copy>
    </xsl:template>

</xsl:stylesheet>