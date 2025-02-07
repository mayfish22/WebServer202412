//打開購物車
async function openShopcart() {
    // 取得目前所有品項
    // 設定請求的配置
    const settings = {
        headers: {
            'user-agent': navigator.userAgent, // 設定用戶代理
            'content-type': 'application/json' // 設定內容類型為 JSON
        },
        method: 'Post', // 設定請求方法為 POST
    };

    // 發送請求到後端以加入購物車
    const fetchResponse = await fetch(`/CartItem/GetCartItems/`, settings); // 發送請求到指定的 URL
    const shopcartPage = await fetchResponse.text(); 

    const productCount = $(shopcartPage).find('tr.productItem').length;

    Swal.fire({
        width: "90%",
        title: "購物車",
        html: shopcartPage,
        cancelButtonText: "關閉",
        showCancelButton: true,
        confirmButtonText: "訂單預覽",
        showConfirmButton: productCount > 0,
        showLoaderOnConfirm: true,
        didOpen: async () => {
            const $partial = $('#swal2-html-container');
            /*--------------------------------------------------*/
            $partial.on('click', '.btn-number', async function (e) {
                e.preventDefault();

                const fieldName = $(e.target).closest('.btn-number').attr('data-field');
                const type = $(e.target).closest('.btn-number').attr('data-type');
                const input = $partial.find("input[name='" + fieldName + "']");
                const currentVal = parseInt(input.val());
                const min = parseInt(input.attr('min'));
                const max = parseInt(input.attr('max'));

                if (!isNaN(currentVal)) {
                    if (type == 'minus') {
                        if (currentVal > min) {
                            input.val(currentVal - 1).change();
                        }
                        if (parseInt(input.val()) == min) {
                            $(e.target).attr('disabled', true);
                        }
                    } else if (type == 'plus') {
                        if (currentVal < max) {
                            input.val(currentVal + 1).change();
                        }
                        if (parseInt(input.val()) == max) {
                            $(e.target).attr('disabled', true);
                        }
                    }
                } else {
                    input.val(0);
                }
            });
            $partial.on('focus', '.input-number', function (e) {
                $(e.target).data('oldValue', $(e.target).val());
            });
            $partial.on('change', '.input-number', async function (e) {
                const productId = $(e.target).attr('data-productId');
                const minValue = parseInt($(e.target).attr('min'));
                const maxValue = parseInt($(e.target).attr('max'));
                const valueCurrent = parseInt($(e.target).val());

                const name = $(e.target).attr('name');

                if (valueCurrent >= minValue) {
                    $partial.find(".btn-number[data-type='minus'][data-field='" + name + "']").removeAttr('disabled')
                } else {
                    //Swal.fire({
                    //    icon: 'error',
                    //    title: `最小值為${minValue}`
                    //});
                    $(e.target).val($(e.target).data('oldValue'));
                    return;
                }
                if (valueCurrent <= maxValue) {
                    $partial.find(".btn-number[data-type='plus'][data-field='" + name + "']").removeAttr('disabled')
                } else {
                    //Swal.fire({
                    //    icon: 'error',
                    //    title: `最大值為${maxValue}`
                    //});
                    $(e.target).val($(e.target).data('oldValue'));
                    return;
                }

                // 設定請求的配置
                const settings = {
                    headers: {
                        'user-agent': navigator.userAgent, // 設定用戶代理
                        'content-type': 'application/json' // 設定內容類型為 JSON
                    },
                    method: 'Post', // 設定請求方法為 POST
                };

                const fetchResponse = await fetch(`/CartItem/SeqCartItemQuantity/${productId}/${valueCurrent}`, settings); // 發送請求到指定的 URL
                const result = await fetchResponse.json(); // 解析回應為 JSON 格式
                $('span.badge.badge-center').text(result.count); // 更新購物車數量
            });
            $partial.on('keydown', '.input-number', function (e) {
                // Allow: backspace, delete, tab, escape, enter and .
                if ($.inArray(e.keyCode, [46, 8, 9, 27, 13, 190]) !== -1 ||
                    // Allow: Ctrl+A
                    (e.keyCode == 65 && e.ctrlKey === true) ||
                    // Allow: home, end, left, right
                    (e.keyCode >= 35 && e.keyCode <= 39)) {
                    // let it happen, don't do anything
                    return;
                }
                // Ensure that it is a number and stop the keypress
                if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
                    e.preventDefault();
                }
            });
            /*--------------------------------------------------*/
        },
        allowOutsideClick: async () => !Swal.isLoading()
    }).then(async (result) => {
        if (result.isConfirmed) {
            // 訂單預覽
            await openOrderPreview();
        }
    });
}

//移除購物車品項
async function removeShopCartItem(cartItemId) {
    const $partial = $('#swal2-html-container');
    $partial.find(`tr.stock-card[data-id="${cartItemId}"]`).remove();

    // 設定請求的配置
    const settings = {
        headers: {
            'user-agent': navigator.userAgent, // 設定用戶代理
            'content-type': 'application/json' // 設定內容類型為 JSON
        },
        method: 'Post', // 設定請求方法為 POST
    };

    const fetchResponse = await fetch(`/CartItem/RemoveCartItem/${cartItemId}`, settings); // 發送請求到指定的 URL
    const result = await fetchResponse.json(); // 解析回應為 JSON 格式
    $('span.badge.badge-center').text(result.count); // 更新購物車數量
}

//預覽訂單
async function openOrderPreview(){
    // 設定請求的配置
    const settings = {
        headers: {
            'user-agent': navigator.userAgent, // 設定用戶代理
            'content-type': 'application/json' // 設定內容類型為 JSON
        },
        method: 'Post', // 設定請求方法為 POST
    };

    const fetchResponse = await fetch(`/CartItem/GetOrderPreview/`, settings); // 發送請求到指定的 URL
    const page = await fetchResponse.text();

    Swal.fire({
        width: "90%",
        title: "訂單預覽",
        html: page,
        cancelButtonText: "關閉",
        showCancelButton: true,
        confirmButtonText: "結帳",
        showLoaderOnConfirm: true,
        didOpen: async () => {
            
        },
        allowOutsideClick: async () => !Swal.isLoading()
    }).then(async (result) => {
        if (result.isConfirmed) {
            // 結帳
            await checkout();
        }
    });
}

//結帳
async function checkout() {
    const $partial = $('#swal2-html-container');
    const remark = $partial.find('#Order_Remark').val();

    // 獲取協議
    const protocol = window.location.protocol;
    // 獲取域名
    const hostname = window.location.hostname;
    // 獲取端口（如果有的話）
    const port = window.location.port ? `:${window.location.port}` : '';
    // 組合完整的域名
    const fullDomain = `${protocol}//${hostname}${port}`;

    // 設定請求的配置
    const settings = {
        headers: {
            'user-agent': navigator.userAgent, // 設定用戶代理
            'content-type': 'application/json' // 設定內容類型為 JSON
        },
        method: 'Post', // 設定請求方法為 POST
        body: JSON.stringify({ remark: remark, fullDomain: fullDomain })
    };

    const fetchResponse = await fetch(`/CartItem/Checkout/`, settings); // 發送請求到指定的 URL
    const result = await fetchResponse.json();
    console.log(result);

    // 初始化 LIFF SDK，傳入 liffId
    liff.init({
        liffId: result.liffId // 設定 LIFF ID
    }).then(async function () {
       
        if (!liff.isLoggedIn()) {
            liff.login(); // 如果未登入，則調用登入方法
        }
        else {
            liff.openWindow({
                url: result.webUrl,
                external: false,
            });
        }
    }).catch(function (error) {
        // 如果初始化失敗，則捕獲錯誤並在控制台中顯示
        console.log(error); // 將錯誤輸出到控制台
        alert(error);
    });
}

//打開訂單記錄
async function openOrderHistory() {
    // 取得目前所有品項
    // 設定請求的配置
    const settings = {
        headers: {
            'user-agent': navigator.userAgent, // 設定用戶代理
            'content-type': 'application/json' // 設定內容類型為 JSON
        },
        method: 'Post', // 設定請求方法為 POST
    };

    // 發送請求到後端以加入購物車
    const fetchResponse = await fetch(`/CartItem/GetOrderHistory/`, settings); // 發送請求到指定的 URL
    const page = await fetchResponse.text();

    Swal.fire({
        width: "90%",
        title: "訂購記錄",
        html: page,
        cancelButtonText: "關閉",
        showCancelButton: true,
        confirmButtonText: "XXX",
        showConfirmButton: false,
        showLoaderOnConfirm: true,
        didOpen: async () => {
            
        },
        allowOutsideClick: async () => !Swal.isLoading()
    }).then(async (result) => {
        //if (result.isConfirmed) {
        //    // 訂單預覽
        //    await openOrderPreview();
        //}
    });
}