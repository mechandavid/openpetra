// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       Timotheus Pokorra <timotheus.pokorra@solidcharity.com>
//       Christopher Jäkel <cj@tbits.net>
//
// Copyright 2017-2018 by TBits.net
// Copyright 2019 by SolidCharity.com
//
// This file is part of OpenPetra.
//
// OpenPetra is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.  If not, see <http://www.gnu.org/licenses/>.
//

$('document').ready(function () {
	display_dropdownlist();
	load_preset();
});

function display_dropdownlist() {
	// x is search
	let x = {};
	x['ALedgerNumber'] = window.localStorage.getItem('current_ledger');

	api.post('serverMFinance.asmx/TBankImportWebConnector_GetImportedBankStatements', x).then(function (data) {
		data = JSON.parse(data.data.d);
		// on reload, clear content
		let field = $('#bank_number_id').html('');
		for (item of data.result) {
			field.append( $('<option value="'+item.a_statement_key_i+'">'+
				item.a_filename_c + ' ' + printJSONDate(item.a_date_d) + '</option>') );
		}
	})
}

function load_preset() {
	let x = {};
	api.post('serverMFinance.asmx/TBankImportWebConnector_ReadSettings', x).then(function (data) {
		data = JSON.parse(data.data.d);
		data['a_bank_account_name_c'] = data['ABankAccountCode'];
		format_tpl($('#tabsettings'), data);
	});
}

function save_preset() {
	var x = extract_data($('#tabsettings'));
	api.post('serverMFinance.asmx/TBankImportWebConnector_SaveSettings', x).then(function (data) {
		data = JSON.parse(data.data.d);
	});
}

function display_list() {
	let x = {};
	x['ALedgerNumber'] = window.localStorage.getItem('current_ledger');
	x['AStatementKey'] = $('#bank_number_id').val();
	x['AMatchAction'] = $('#match_status_id').val();
	api.post('serverMFinance.asmx/TBankImportWebConnector_GetTransactions', x).then(function (data) {
		data = JSON.parse(data.data.d);
		// on reload, clear content
		let field = $('#browse_container').html('');
		for (item of data.result) {
			format_item(item);
		}
		format_currency(data.ACurrencyCode);
		format_date();
		$('#trans_total_debit').text(printCurrency(data.ATotalDebit, data.ACurrencyCode));
		$('#trans_total_credit').text(printCurrency(data.ATotalCredit, data.ACurrencyCode));
	})
}

function updateTransaction(StatementKey, OrderId) {
	// somehow the original window stays gray when we return from this modal.
	$('.modal-backdrop').remove();
	edit_gift_trans(StatementKey, OrderId);
}

function format_item(item) {
	let row = format_tpl($("[phantom] .tpl_row").clone(), item);
	// let view = format_tpl($("[phantom] .tpl_view").clone(), item);
	// row.find('.collapse_col').append(view);
	$('#browse_container').append(row);
}

/////

function new_trans_detail(trans_order) {
	let x = {
		"ALedgerNumber":window.localStorage.getItem('current_ledger'),
		"AStatementKey":$('#bank_number_id').val(),
		"AOrderNumber": trans_order
	};
	api.post('serverMFinance.asmx/TBankImportWebConnector_LoadTransactionAndDetails', x).then(function (data) {
		parsed = JSON.parse(data.data.d);
		let p = parsed.ATransactions[0];
		p['p_donor_key_n'] = getKeyValue($('.tpl_edit_trans'), 'p_donor_key_n');
		p['a_detail_i'] = $('#modal_space .tpl_edit_trans .detail_col > *').length;
		let tpl_edit_raw = format_tpl( $('[phantom] .tpl_edit_trans_detail').clone(), p );
		$('#modal_space').append(tpl_edit_raw);
		let sclass = $('#modal_space > .tpl_edit_trans [name=MatchAction]:checked').val();
		tpl_edit_raw.append( $('<input type=hidden name=AMatchAction value="'+ sclass + '">') );
		tpl_edit_raw.find('[action]').val('create');
		tpl_edit_raw.modal('show');
		update_requireClass(tpl_edit_raw, sclass);
	})
}

/////

function edit_gift_trans(statement_key, trans_order) {
	let x = {
		"ALedgerNumber":window.localStorage.getItem('current_ledger'),
		"AStatementKey":statement_key,
		"AOrderNumber": trans_order
	};
	// on open of a edit modal, we get new data,
	// so everything is up to date and we don't have to load it, if we only search
	api.post('serverMFinance.asmx/TBankImportWebConnector_LoadTransactionAndDetails', x).then(function (data) {
		parsed = JSON.parse(data.data.d);
		transaction = parsed.ATransactions[0];
		transaction['p_donor_name_c'] = transaction['DonorKey'] + ' ' + transaction['DonorName'];
		transaction['p_donor_key_n'] = transaction['DonorKey'];
		let tpl_edit_raw = format_tpl( $('[phantom] .tpl_edit_trans').clone(), transaction);

		for (detail of parsed.ADetails) {
			let tpl_trans_detail = format_tpl( $('[phantom] .tpl_trans_detail_row').clone(), detail );
			tpl_edit_raw.find('.detail_col').append(tpl_trans_detail);
		}

		$('#modal_space').html(tpl_edit_raw);
		tpl_edit_raw.find('[action]').val('update');
		tpl_edit_raw.modal('show');
		update_requireClass(tpl_edit_raw, parsed.ATransactions[0].MatchAction);

	})
}

function edit_gift_trans_detail(statement_id, order_id, detail_id) {

	let x = {
		"ALedgerNumber":window.localStorage.getItem('current_ledger'),
		"AStatementKey":statement_id,
		"AOrder": order_id,
		"ADetail": detail_id
	};
	api.post('serverMFinance.asmx/TBankImportWebConnector_LoadTransactionDetail', x).then(function (data) {
		parsed = JSON.parse(data.data.d);
		parsed.TransactionDetail[0]['p_donor_key_n'] = getKeyValue($('.tpl_edit_trans'), 'p_donor_key_n');
		let tpl_edit_raw = format_tpl( $('[phantom] .tpl_edit_trans_detail').clone(), parsed.TransactionDetail[0] );
		let sclass = $('#modal_space > .modal [name=MatchAction]:checked').val();
		tpl_edit_raw.append( $('<input type=hidden name=AMatchAction value="'+ sclass + '">') );
		$('#modal_space').append(tpl_edit_raw);
		tpl_edit_raw.find('[action]').val('update');
		tpl_edit_raw.modal('show');
		update_requireClass(tpl_edit_raw, sclass);
	})
}

/////

function save_edit_trans(obj_modal) {
	let obj = $(obj_modal).closest('.modal');
	let mode = obj.find('[action]').val();

	// extract information from a jquery object
	let payload = translate_to_server( extract_data(obj) );
 	payload['action'] = mode;
	payload['ALedgerNumber'] = window.localStorage.getItem('current_ledger');
	payload["AStatementKey"] = $('#bank_number_id').val();

	api.post('serverMFinance.asmx/TBankImportWebConnector_MaintainTransaction', payload).then(function (result) {
		parsed = JSON.parse(result.data.d);
		if (parsed.result == true) {
			display_message(i18next.t('forms.saved'), "success");
			$('#modal_space .modal').modal('hide');
			display_list();
		}
		else if (parsed.result == false) {
			display_error(parsed.AVerificationResult);
		}
	});
}

function save_edit_trans_detail(obj_modal) {
	let obj = $(obj_modal).closest('.modal');
	let mode = obj.find('[action]').val();

	// extract information from a jquery object
	let payload = translate_to_server( extract_data(obj) );
	payload['action'] = mode;
 	payload['ALedgerNumber'] = window.localStorage.getItem('current_ledger');
	payload['AStatementKey'] = $('#bank_number_id').val();

	api.post('serverMFinance.asmx/TBankImportWebConnector_MaintainTransactionDetail', payload).then(function (result) {
		parsed = JSON.parse(result.data.d);
		if (parsed.result == true) {
			display_message(i18next.t('forms.saved'), "success");
			updateTransaction(payload['AStatementKey'], payload['AOrder']);
		}
		else if (parsed.result == false) {
			display_error(parsed.AVerificationResult);
		}

	});

}

/////

function delete_trans_detail(obj_modal) {
	let obj = $(obj_modal).closest('.modal');
	let payload = translate_to_server( extract_data(obj) );
	payload['action'] = "delete";
	payload['AStatementKey'] = $('#bank_number_id').val();
	payload['ALedgerNumber'] = window.localStorage.getItem('current_ledger');
	api.post('serverMFinance.asmx/TBankImportWebConnector_MaintainTransactionDetail', payload).then(function (data) {
		parsed = JSON.parse(data.data.d);
		if (parsed.result) {
			display_message(i18next.t('forms.deleted'), "success");
			updateTransaction(payload['AStatementKey'], payload['AOrder']);
		} else {
			display_error(parsed.AVerificationResult);
		}

	});
}

/////

function import_file(self) {
	self = $(self);
	var filename = self.val();

	// see http://www.html5rocks.com/en/tutorials/file/dndfiles/
	if (window.File && window.FileReader && window.FileList && window.Blob) {
		//alert("Great success! All the File APIs are supported.");
	} else {
	  alert('The File APIs are not fully supported in this browser.');
	}

	var settings = extract_data($('#tabsettings'));

	var reader = new FileReader();

	reader.onload = function (event) {

		p = {
			'ALedgerNumber': window.localStorage.getItem('current_ledger'),
			'ABankAccountCode': settings['ABankAccountCode'],
			'ABankStatementFilename': filename,
			'ACSVContent': event.target.result,
			'ASeparator': settings['ASeparator'],
			'ADateFormat': settings['ADateFormat'],
			"ANumberFormat": settings['ANumberFormat'],
			"AStartAfterLine": settings['AStartAfterLine'],
			"AColumnMeaning": settings['AColumnMeaning']
			};

		api.post('serverMFinance.asmx/TBankImportWebConnector_ImportFromCSVFile', p)
		.then(function (result) {
			result = JSON.parse(result.data.d);
			result = result.result;
			if (result == true) {
				display_message(i18next.t('BankImport.upload_success'), "success");
				display_dropdownlist();
			} else {
				display_message(i18next.t('BankImport.upload_fail'), "fail");
			}
		})
		.catch(error => {
			//console.log(error.response)
			display_message(i18next.t('BankImport.upload_fail'), "fail");
		});

	}
	// Read in the file as a data URL.
	reader.readAsText(self[0].files[0], settings['AFileEncoding']);

};

function transform_to_gl() {
 let x = {
	 ALedgerNumber: window.localStorage.getItem('current_ledger'),
	 AStatementKey: $('#bank_number_id').val(),
 };
	api.post('serverMFinance.asmx/TBankImportWebConnector_CreateGLBatch', x).then(function (data) {
		let parsed = JSON.parse(data.data.d);
		if (parsed.result == true) {
			display_message( i18next.t('forms.saved'), 'success' )
		}
		else {
			display_error( parsed.AVerificationResult );
		}
	});


}

function transform_to_gift() {
 let x = {
	 ALedgerNumber: window.localStorage.getItem('current_ledger'),
	 AStatementKey: $('#bank_number_id').val(),
 };
	api.post('serverMFinance.asmx/TBankImportWebConnector_CreateGiftBatch', x).then(function (data) {
		let parsed = JSON.parse(data.data.d);
		if (parsed.result == true) {
			display_message( i18next.t('forms.saved'), 'success' )
		}
		else {
			display_error( parsed.AVerificationResult );
		}
	});

}
