mergeInto(LibraryManager.library, {

  AbrirEnMismaPestana: function (url) {
    // Convertimos el puntero de memoria de Unity a texto real
    var urlString = UTF8ToString(url);
    
    // CAMBIO AQUÍ: "_top" le dice al navegador que use la ventana completa,
    // rompiendo la restricción del iframe.
    window.open(urlString, "_top");
  },

});