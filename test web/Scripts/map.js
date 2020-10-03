

var script = document.createElement('script');
script.src = 'https://ajax.googleapis.com/ajax/libs/jquery/3.5.1/jquery.min.js';
script.type = 'text/javascript';
document.getElementsByTagName('head')[0].appendChild(script);

function ObservableArray(items) {
    var _self = this,
        _array = [],
        _handlers = {
            itemadded: [],
            itemremoved: [],
            itemset: []
        };

    function defineIndexProperty(index) {
        if (!(index in _self)) {
            Object.defineProperty(_self, index, {
                configurable: true,
                enumerable: true,
                get: function () {
                    return _array[index];
                },
                set: function (v) {
                    _array[index] = v;
                    raiseEvent({
                        type: "itemset",
                        index: index,
                        item: v
                    });
                }
            });
        }
    }

    function raiseEvent(event) {
        _handlers[event.type].forEach(function (h) {
            h.call(_self, event);
        });
    }

    Object.defineProperty(_self, "addEventListener", {
        configurable: false,
        enumerable: false,
        writable: false,
        value: function (eventName, handler) {
            eventName = ("" + eventName).toLowerCase();
            if (!(eventName in _handlers)) throw new Error("Invalid event name.");
            if (typeof handler !== "function") throw new Error("Invalid handler.");
            _handlers[eventName].push(handler);
        }
    });

    Object.defineProperty(_self, "removeEventListener", {
        configurable: false,
        enumerable: false,
        writable: false,
        value: function (eventName, handler) {
            eventName = ("" + eventName).toLowerCase();
            if (!(eventName in _handlers)) throw new Error("Invalid event name.");
            if (typeof handler !== "function") throw new Error("Invalid handler.");
            var h = _handlers[eventName];
            var ln = h.length;
            while (--ln >= 0) {
                if (h[ln] === handler) {
                    h.splice(ln, 1);
                }
            }
        }
    });

    Object.defineProperty(_self, "push", {
        configurable: false,
        enumerable: false,
        writable: false,
        value: function () {
            var index;
            for (var i = 0, ln = arguments.length; i < ln; i++) {
                index = _array.length;
                _array.push(arguments[i]);
                defineIndexProperty(index);
                raiseEvent({
                    type: "itemadded",
                    index: index,
                    item: arguments[i]
                });
            }
            return _array.length;
        }
    });

    Object.defineProperty(_self, "pop", {
        configurable: false,
        enumerable: false,
        writable: false,
        value: function () {
            if (_array.length > -1) {
                var index = _array.length - 1,
                    item = _array.pop();
                delete _self[index];
                raiseEvent({
                    type: "itemremoved",
                    index: index,
                    item: item
                });
                return item;
            }
        }
    });

    Object.defineProperty(_self, "unshift", {
        configurable: false,
        enumerable: false,
        writable: false,
        value: function () {
            for (var i = 0, ln = arguments.length; i < ln; i++) {
                _array.splice(i, 0, arguments[i]);
                defineIndexProperty(_array.length - 1);
                raiseEvent({
                    type: "itemadded",
                    index: i,
                    item: arguments[i]
                });
            }
            for (; i < _array.length; i++) {
                raiseEvent({
                    type: "itemset",
                    index: i,
                    item: _array[i]
                });
            }
            return _array.length;
        }
    });

    Object.defineProperty(_self, "shift", {
        configurable: false,
        enumerable: false,
        writable: false,
        value: function () {
            if (_array.length > -1) {
                var item = _array.shift();
                delete _self[_array.length];
                raiseEvent({
                    type: "itemremoved",
                    index: 0,
                    item: item
                });
                return item;
            }
        }
    });

    Object.defineProperty(_self, "splice", {
        configurable: false,
        enumerable: false,
        writable: false,
        value: function (index, howMany /*, element1, element2, ... */) {
            var removed = [],
                item,
                pos;

            index = index == null ? 0 : index < 0 ? _array.length + index : index;

            howMany = howMany == null ? _array.length - index : howMany > 0 ? howMany : 0;

            while (howMany--) {
                item = _array.splice(index, 1)[0];
                removed.push(item);
                delete _self[_array.length];
                raiseEvent({
                    type: "itemremoved",
                    index: index + removed.length - 1,
                    item: item
                });
            }

            for (var i = 2, ln = arguments.length; i < ln; i++) {
                _array.splice(index, 0, arguments[i]);
                defineIndexProperty(_array.length - 1);
                raiseEvent({
                    type: "itemadded",
                    index: index,
                    item: arguments[i]
                });
                index++;
            }

            return removed;
        }
    });

    Object.defineProperty(_self, "length", {
        configurable: false,
        enumerable: false,
        get: function () {
            return _array.length;
        },
        set: function (value) {
            var n = Number(value);
            var length = _array.length;
            if (n % 1 === 0 && n >= 0) {
                if (n < length) {
                    _self.splice(n);
                } else if (n > length) {
                    _self.push.apply(_self, new Array(n - length));
                }
            } else {
                throw new RangeError("Invalid array length");
            }
            _array.length = n;
            return value;
        }
    });

    Object.getOwnPropertyNames(Array.prototype).forEach(function (name) {
        if (!(name in _self)) {
            Object.defineProperty(_self, name, {
                configurable: false,
                enumerable: false,
                writable: false,
                value: Array.prototype[name]
            });
        }
    });

    if (items instanceof Array) {
        _self.push.apply(_self, items);
    }
}

var queue = new ObservableArray([]);

queue.addEventListener("itemadded", function (e) {
    var ten = e.item.TenNguoiDi;
    var noidi = e.item.NoiDi;
    var noiden = e.item.NoiDen;
    var gioden = e.item.GioDen.Hours + ":" + e.item.GioDen.Minutes;
    var giove = e.item.GioVe.Hours + ":" + e.item.GioVe.Minutes;

    var newitem = "<div class=\"item\">";
    newitem += "<div class=\"nguoidi\">" + ten + "</div>";
    newitem += "<div class=\"noidi\">" + noidi + "</div>";
    newitem += "<div class=\"icon\">mũi tên</div>";
    newitem += "<div class=\"noiden\">(" + gioden + " - " + giove + ")<br/>" + noiden + "</div>";
    newitem += "</div>";

    alert(newitem);
    document.getElementById("danhsach").innerHTML += newitem;
    capnhatgrid();
});

queue.addEventListener("itemremoved", function (e) {
    var items = document.getElementById("danhsach").children;
    items[e.index].remove();
    capnhatgrid();
});

function capnhatgrid(){
    var x = document.getElementById("danhsach").childElementCount;
    var s = "";
    for(var i = 0; i < x /2; i++){
            s += "auto ";
    }
    $("#danhsach").css("grid-template-columns", s);
}

$(document).ready(function () {
    //queue = new ObservableArray([]);
    //queue.push({ TenNguoiDi: "thuận", NoiDi : "nơi đi 1", NoiDen :  "Nơi đến 1", GioDen : "08:00", GioVe : "16:00" });
    //queue.push({ TenNguoiDi: "quang", NoiDi : "nơi đi 2", NoiDen :  "Nơi đến 2", GioDen : "07:00", GioVe : "16:00" });
    $("#buttonthemlich").click(function () {

    });

    $("#buttonxoalich").click(function () {
        if (queue.length > 0)
            queue.splice(0, 1);
    });
});

//var map;

//var curvature = 0.5; // how curvy to make the arc


//function init() {
//    var Map = google.maps.Map,
//        LatLng = google.maps.LatLng,
//        LatLngBounds = google.maps.LatLngBounds,
//        Marker = google.maps.Marker,
//        Point = google.maps.Point;

//    // This is the initial location of the points
//    // (you can drag the markers around after the map loads)
//    var pos1 = new LatLng(23.634501, -102.552783);
//    var pos2 = new LatLng(17.987557, -92.929147);

//    var bounds = new LatLngBounds();
//    bounds.extend(pos1);
//    bounds.extend(pos2);

//    map = new Map(document.getElementById('map-canvas'), {
//        center: bounds.getCenter(),
//        zoom: 12
//    });
//    map.fitBounds(bounds);

//    var markerP1 = new Marker({
//        position: pos1,
//        label: "1",
//        draggable: true,
//        map: map
//    });
//    var markerP2 = new Marker({
//        position: pos2,
//        label: "2",
//        draggable: true,
//        map: map
//    });

//    var curveMarker;

//    function updateCurveMarker() {
//        var pos1 = markerP1.getPosition(), // latlng
//            pos2 = markerP2.getPosition(),
//            projection = map.getProjection(),
//            p1 = projection.fromLatLngToPoint(pos1), // xy
//            p2 = projection.fromLatLngToPoint(pos2);

//        // Calculate the arc.
//        // To simplify the math, these points 
//        // are all relative to p1:
//        var e = new Point(p2.x - p1.x, p2.y - p1.y), // endpoint (p2 relative to p1)
//            m = new Point(e.x / 2, e.y / 2), // midpoint
//            o = new Point(e.y, -e.x), // orthogonal
//            c = new Point( // curve control point
//                m.x + curvature * o.x,
//                m.y + curvature * o.y);

//        var pathDef = 'M 0,0 ' +
//            'q ' + c.x + ',' + c.y + ' ' + e.x + ',' + e.y;

//        var zoom = map.getZoom(),
//            scale = 1 / (Math.pow(2, -zoom));

//        var symbol = {
//            path: pathDef,
//            scale: scale,
//            strokeWeight: 2,
//            fillColor: 'none'
//        };

//        if (!curveMarker) {
//            curveMarker = new Marker({
//                position: pos1,
//                clickable: false,
//                icon: symbol,
//                zIndex: 0, // behind the other markers
//                map: map
//            });
//        } else {
//            curveMarker.setOptions({
//                position: pos1,
//                icon: symbol,
//            });
//        }
//    }

//    google.maps.event.addListener(map, 'projection_changed', updateCurveMarker);
//    google.maps.event.addListener(map, 'zoom_changed', updateCurveMarker);

//    google.maps.event.addListener(markerP1, 'position_changed', updateCurveMarker);
//    google.maps.event.addListener(markerP2, 'position_changed', updateCurveMarker);
//}

////google.maps.event.addDomListener(window, 'load', init);