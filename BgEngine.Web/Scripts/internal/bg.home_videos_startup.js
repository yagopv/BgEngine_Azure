﻿/*==============================================================================
* This file is part of BgEngine.
*
* BgEngine is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* BgEngine is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with BgEngine. If not, see <http://www.gnu.org/licenses/>.
*==============================================================================
* Copyright (c) 2011 Yago Pérez Vázquez
* Version: 1.0
*==============================================================================*/

(function ($) {
    $.home_videos_startup = function (videourl, autocompleteurl, message, loadingmessage) {
        $(".bg-button-search").button({ icons: { primary: "ui-icon-zoomin" }, text: false });
        $("#searchbutton").click(function () {
            var url = videourl + "?searchstring=" + encodeURI($("#searchvideo").val()) + " #public-videos>*";
            $("#public-videos").load(url, connectPager);
            return false;
        });
        var needsDelay = false;
        $("#searchvideo").keyup(function (event) {
            if ((!needsDelay) && (!event.shiftKey) && (!event.altKey) && (event.keyCode != '16') && (event.keyCode != '18')
					&& (event.keyCode != '37') && (event.keyCode != '38') && (event.keyCode != '39') && (event.keyCode != '40')) {
                needsDelay = true;
                setTimeout(function () { needsDelay = false; }, "1000");
                var url = videourl + "?searchstring=" + encodeURI($(this).val()) + " #public-videos>*";
                $("#public-videos").load(url, connectPager);
            }
        });
        $("#searchvideo").focusin(function () { $(this).addClass("ui-state-hover") });
        $("#searchvideo").focusout(function () { $(this).removeClass("ui-state-hover") });
        $("#searchvideo").watermark(message, { className: "watermark", useNative: false });
        $("#searchvideo").autocomplete({
            source: autocompleteurl,
            select: function (event, ui) {
                var url = videourl + "?searchstring=" + encodeURI(ui.item.label) + " #public-videos>*";
                $("#public-videos").load(url, connectPager);
            }
        });

        $("#pager a[href*=Video]").live("click", function () {
            var url = $(this).attr("href");
            $("#public-videos").load(url + " #public-videos>*", connectPager);
            return false;
        });
        $("#public-videos").ajaxStart(function () {
            $("#public-videos").block({ css: {
                border: 'none',
                padding: '15px',
                backgroundColor: '#000',
                '-webkit-border-radius': '10px',
                '-moz-border-radius': '10px',
                opacity: .5,
                color: '#fff'
            },
                message: loadingmessage
            });
        });
        $("#public-videos").ajaxComplete(function () {
            $("#public-videos").unblock();
        });
        $(".video-container").live("hover",
            function () {
                $(this).toggleClass("ui-state-highlight");
                $(".video-container").not(this).removeClass("ui-state-highlight");
            },
            function () {
                $(this).removeClass("ui-state-highlight");
            }
        ); 
        connectPager();
    };

    function connectPager() {
        $("#pager").buttonset();
        $("#pager a").each(function () {
            var myHref = $(this).attr("href");
            if (myHref != null) {
                $(this).addClass("ui-state-active");
                $(this).removeClass("ui-state-disabled");
                $(this).mouseover(function () {
                    $(this).removeClass("ui-state-active");
                }).mouseout(function () {
                    $(this).addClass("ui-state-active");
                });
                $(this).click(function () {
                    $("#pager a").removeClass("ui-state-active");
                    $(this).addClass("ui-state-active");
                });
            }
            else {
                $(this).addClass("ui-state-disabled");
                $(this).removeClass("ui-state-active");
            }
        });
        $(".tooltip, .tooltip-default").tipTip();
        $(".tooltip-ajax").tipTip({
            content: function (data) {
                $.ajax({
                    url: $(this).attr("href"),
                    success: function (response) {
                        data.content.html(response);
                    }
                });
                return Globalize.localize("loading", "@CultureHelper.GetNeutralCulture(CultureHelper.GetCurrentCulture())");
            }
        });
        $(".tooltip-ajax").click(function () { return false; });
    }
})(jQuery);