// CSP satır içi script'e izin vermediği için davranışlar data-* öznitelikleriyle buradan bağlanır.
(() => {
    "use strict";

    // <form data-confirm="..."> gönderilmeden önce onay ister (silme, yayından kaldırma).
    document.addEventListener("submit", event => {
        const form = event.target;
        const message = event.submitter?.dataset.confirm ?? form.dataset.confirm;
        if (message && !window.confirm(message)) {
            event.preventDefault();
        }
    });

    // <ol data-sortable> içindeki öğeler sürüklenerek sıralanır; bırakınca form "Sırayı kaydet" ile gönderilir.
    // Gizli ImageIds alanları öğelerle birlikte taşındığı için gönderilen sıra ekrandaki sıradır.
    document.querySelectorAll("[data-sortable]").forEach(list => {
        let dragged = null;

        list.querySelectorAll("li").forEach(item => {
            item.draggable = true;
            item.addEventListener("dragstart", () => {
                dragged = item;
                item.classList.add("opacity-50");
            });
            item.addEventListener("dragend", () => {
                item.classList.remove("opacity-50");
                dragged = null;
            });
            item.addEventListener("dragover", event => {
                event.preventDefault();
                if (!dragged || dragged === item) {
                    return;
                }
                const rect = item.getBoundingClientRect();
                const after = event.clientX > rect.left + rect.width / 2;
                item.parentNode.insertBefore(dragged, after ? item.nextSibling : item);
            });
        });

        list.addEventListener("drop", event => {
            event.preventDefault();
            list.closest("form")?.querySelector("[data-sortable-save]")?.removeAttribute("hidden");
        });
    });
})();
