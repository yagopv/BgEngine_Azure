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
    $.index_startup = function (text) {
        $("table").grid();
        $("th a").button({ icons: { primary: "ui-icon-triangle-2-n-s" }, text: true });
        $("th a").live("click", function () { return false; });
        $("#grid-container").ajaxStart(function () {
            $("#grid-container").block({ css: {
                border: 'none',
                padding: '15px',
                backgroundColor: '#000',
                '-webkit-border-radius': '10px',
                '-moz-border-radius': '10px',
                opacity: .5,
                color: '#fff'
            },
                message: text
            });
        });
        $("#grid-container").ajaxComplete(function () {
            $("#grid-container").unblock();
            $("table").grid();
            ajaxconnect();
        });

        $(".multiselect-check input[type='checkbox']").bind("enhancedCheckbox_click", function () {
            if ($(this).attr("checked")) {
                $.cookie($(this).attr("id"), "true");
                hideShowColumn($(this).attr("data-column"), true);
            }
            else {
                $.cookie($(this).attr("id"), "false");
                hideShowColumn($(this).attr("data-column"), false);
            }
        });

        $("#options").click(function () {
            $("#options-container").slideToggle("slow");
            return false;
        });

        $(".comment a").live("click", function () {
            var $container = $(this).parent().parent();
            $.post($(this).attr("href"), { id: $(this).parent().attr("data-commentid") },
					function (data, status) {
					    if (data.result == "error") {
					        createGrowl(data.text);
					    }
					    else {
					        $container.children().toggle();
					    }
					}
				);
            return false;
        });

        loadCheckBoxes();
        loadColumns();
        ajaxconnect();
        enhanceCheckboxes();
    };

    function ajaxconnect() {
        $(".newsletter-start").button({ icons: { primary: "ui-icon-mail-closed" }, text: false });
        $(".bg-button-grid-edit").button({ icons: { primary: "ui-icon-pencil" }, text: false });                
        $(".bg-button-grid-delete").button({ icons: { primary: "ui-icon-circle-close" }, text: false });
        $(".bg-button-grid-zoom").button({ icons: { primary: "ui-icon-zoomin" }, text: false });
        $("th a").button({ icons: { primary: "ui-icon-triangle-2-n-s" }, text: true });
        $(".options").buttonset();
        loadColumns();
        reconnectTooltips();
    }

    function hideShowColumn(column, show) {
        if (show) {
            $("table thead tr th:nth-child(" + column + ")").show();
            $("table tbody tr td:nth-child(" + column + ")").show();
        }
        else {
            $("table thead tr th:nth-child(" + column + ")").hide();
            $("table tbody tr td:nth-child(" + column + ")").hide();
        }
    }

    function loadCheckBoxes() {
        $(".multiselect-check input[type='checkbox']").each(function () {
            var cookieValue = $.cookie($(this).attr("id"));
            if (cookieValue == "true") {
                $(this).attr("checked", "true");
            }
            else {
                if (cookieValue == "false") {
                    $(this).removeAttr("checked");
                }
                else {
                    if ($(this).attr("data-default") == "true") {
                        $(this).attr("checked", "true");
                    }
                }
            }
        });
    }

    function loadColumns() {
        $(".multiselect-check input[type='checkbox']").each(function () {
            var cookieValue = $.cookie($(this).attr("id"));
            if (cookieValue == "true") {
                hideShowColumn($(this).attr("data-column"), true);
            }
            else {
                if (cookieValue == "false") {
                    hideShowColumn($(this).attr("data-column"), false);
                }
                else {
                    if ($(this).attr("data-default") == "true") {
                        hideShowColumn($(this).attr("data-column"), true);
                    }
                    else {
                        hideShowColumn($(this).attr("data-column"), false);
                    }
                }
            }
        });
    }

    function enhanceCheckboxes() {
        $(".multiselect-check input[type='checkbox']").each(function () {
            var element = this;
            $(element).addClass('ui-state-default ui-corner-all');
            $(element).wrap("<label />");
            $(element).parent("label").after("<span />");
            var parent = $(element).parent("label").next();
            $(element).addClass("ui-helper-hidden");
            parent.css({ width: 16, height: 16, display: "block" });

            parent.wrap("<span class='ui-state-default ui-corner-all' style='display:inline-block;width:16px;height:16px;margin-right:5px;'/>");

            parent.parent().addClass('hover');

            if (element.checked) {
                parent.addClass("ui-icon ui-icon-check");
            }

            parent.parent("span").click(function (event) {
                $(this).toggleClass("ui-state-active");
                parent.toggleClass("ui-icon ui-icon-check");
                $(element).click();
                $(element).trigger("enhancedCheckbox_click");
                return false;
            });
        });
    }

    // Reconnect tooltips after ajax load
    function reconnectTooltips() {
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

    function createGrowl(message) {
        $.blockUI({
            message: message,
            fadeIn: 700,
            fadeOut: 700,
            timeout: 5000,
            showOverlay: false,
            centerY: false,
            css: {
                width: '350px',
                top: '10px',
                left: '',
                right: '10px',
                border: 'none',
                padding: '5px',
                backgroundColor: '#000',
                '-webkit-border-radius': '10px',
                '-moz-border-radius': '10px',
                opacity: .6,
                color: '#fff'
            }
        });
    }
})(jQuery);
