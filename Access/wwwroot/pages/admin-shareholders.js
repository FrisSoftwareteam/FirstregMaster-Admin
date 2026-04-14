"use strict";

var page = {
    init: function () {
        ! function () {
            const t = document.getElementById("tb_holdings");

            if (!t) return;

            var url = t.getAttribute('data-url');

            const e = $(t).DataTable({
                info: !1,
                order: [1, 'asc'],
                ajax: url,
                'processing': true,
                'language': {
                    'loadingRecords': '&nbsp;',
                    'processing': '<span class="spinner-border spinner-border-sm align-middle ms-2"></span>'
                },
                buttons: [{
                    extend: 'excelHtml5',
                    autoFilter: true,
                    sheetName: 'Exported data'
                }],
                createdRow: function (row, data, index) {
                    $(row).attr('id', `tr-${data[4]}`);

                    $('td', row).eq(4).addClass('text-end');
                    $('td', row).eq(4).html(
                        `<a class="btn btn-sm btn-light btn-active-light-primary a_details" data-id="${data[5]}" 
                            data-accno="${data[4]}" data-reg="${data[6]}" onclick="ViewShareholderDetails(${data[5]}, ${data[6]}, ${data[4]})">
                            <span class="fw-bolder">Details</span>
                        </a>`);
                }
            });

            var r = document.getElementById("tb_holdings_search");
            var btexport = document.querySelector("#bt_export");

            var tx = r.value;

            r && r.addEventListener("keyup", (function (t) {
                tx = t.target.value;
                e.search(t.target.value).draw();
            }));

            noUiSlider.create(slider, {
                start: [0, max],
                connect: true,
                range: {
                    "min": 0,
                    "max": max
                }
            });

            slider.noUiSlider.on("update", function (values, handle) {
                if (handle) {
                    max = values[handle];
                    valueMax.innerHTML = values[handle];
                } else {
                    min = values[handle];
                    valueMin.innerHTML = values[handle];
                }
            });

        }()
    }
};

KTUtil.onDOMContentLoaded((function () {
    page.init()
}));

const printButton = document.getElementById('bt_print');
printButton.addEventListener('click', () => {
    window.print();
});

const closeButton = document.getElementById('bt_close');
closeButton.addEventListener('click', () => {
    switchBlock('v_list', 'v-block');
});

const printButtonDivi = document.getElementById('bt_print_dividends');
printButtonDivi.addEventListener('click', () => {
    window.print();
});

const closeButtonDivi = document.getElementById('bt_close_dividends');
closeButtonDivi.addEventListener('click', () => {
    switchBlock('v_list', 'v-block');
});

var ViewShareholderDetails = function (id, reg, accno) {

    let url = $('#hd_details_url').val();

    $.ajax({
        type: "GET",
        url: `${url}/${reg}/${accno}`,
        cache: false,
        success: function (json) {
            //console.log(json);

            setAccountStatement(json, url, reg, accno);
            setDividendHistory(json, url, reg, accno);

            switchBlock('v_details', 'v-block');
        },
        error: function () {
            toastr.error('could not fetch details');
        },
        complete: function () {
        }
    });
}

function setAccountStatement(json, url, reg, accno) {
    $('#sp_name').html(json.name);
    $('#sp_register').html(json.register);

    $('#sp_acc').html(json.accountNo);
    $('#sp_cscs').html(json.clearingNo);
    $('#sp_oldacc').html('');
    $('#sp_address').html(json.address);

    $('#sp_phone').html(json.phone);
    $('#sp_mobile').html(json.mobile);
    $('#sp_email').html(json.email);

    $('#sp_count').html(json.units.length);
    $('#sp_units_s').html(json.units.length > 1 ? 's' : '');

    $('#t_body').html('');
    $('#t_foot').html('');

    let index = 0;
    let balance = 0;

    for (let unit of json.units) {
        index += 1;
        let credit = unit.totalUnits > 0 ? unit.totalUnits : 0;
        let debit = unit.totalUnits < 0 ? Math.abs(unit.totalUnits) : 0;
        balance += (credit - debit);

        $('#t_body').append(
            `<tr>
                <td>${index}</td>
                <td>${unit.certNo ?? '-'}</td>
                <td>${unit.oldCertNo ?? '-'}</td>
                <td>${unit.date}</td>
                <td>${unit.narration ?? '-'}</td>
                <td class="text-end">${credit > 0 ? credit.toLocaleString() : '-'}</td>
                <td class="text-end">${debit > 0 ? debit.toLocaleString() : '-'}</td>
                <td class="text-end">${balance.toLocaleString()}</td>
                <td class="text-center">Active</td>
            </tr>`
        );
    }

    $('#t_foot').append(
        `<tr class="fw-bolder">
            <th></th>
            <th></th>
            <th></th>
            <th></th>
            <th></th>
            <th></th>
            <th class="text-end">Balance:</th>
            <th class="text-end">${json.totalUnits.toLocaleString()}</th>
            <th></th>
        </tr>`
    );

    $('#bt_export_acc').attr('href', `${url}/${reg}/${accno}/download`);
}

function setDividendHistory(json, url, reg, accno) {
    $('#sp_divs_name').html(json.name);
    $('#sp_divs_register').html(json.register);

    $('#sp_divs_acc').html(json.accountNo);
    $('#sp_divs_cscs').html(json.clearingNo);
    $('#sp_divs_oldacc').html('');
    $('#sp_divs_address').html(json.address);

    $('#sp_divs_phone').html(json.phone);
    $('#sp_divs_mobile').html(json.mobile);
    $('#sp_divs_email').html(json.email);

    $('#sp_divs_count').html(json.dividends.length);
    $('#sp_divs_units_s').html(json.dividends.length > 1 ? 's' : '');

    $('#t_divs_body').html('');
    $('#t_divs_foot').html('');

    let index = 0;

    for (let unit of json.dividends) {
        index += 1;

        $('#t_divs_body').append(
            `<tr>
                <td>${index}</td>
                <td>${unit.date}</td>
                <td>${unit.dividendNo ?? '-'}</td>
                <td>${unit.warrantNo ?? '-'}</td>
                <td>${unit.type ?? '-'}</td>
                <td class="text-end">${unit.total ?? '-'}</td>
                <td class="text-end">${unit.gross ?? '-'}</td>
                <td class="text-end">${unit.tax ?? '-'}</td>
                <td class="text-end">${unit.net ?? '-'}</td>
            </tr>`
        );
    }

    $('#bt_export_dividends').attr('href', `${url}/${reg}/${accno}/download`);
}
