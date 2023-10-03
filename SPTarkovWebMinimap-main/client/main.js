import './style.css';
import ImageLayer from 'ol/layer/Image.js';
import Map from 'ol/Map.js';
import Projection from 'ol/proj/Projection.js';
import {toLonLat, fromLonLat} from 'ol/proj';
import Static from 'ol/source/ImageStatic.js';
import View from 'ol/View.js';
import { getCenter } from 'ol/extent.js';
import { Point } from 'ol/geom';
import { Feature, Overlay } from 'ol';
import {Icon, Style, Fill} from 'ol/style.js';
import {Vector as VectorSource} from 'ol/source.js';
import {Vector as VectorLayer} from 'ol/layer.js';
import MousePosition from 'ol/control/MousePosition';
import {Control, defaults as defaultControls} from 'ol/control.js';
import {defaults as defaultInteractions} from 'ol/interaction/defaults';
import {pointerMove} from 'ol/events/condition.js';
import Select from 'ol/interaction/Select.js';
import Popup from 'ol-popup/src/ol-popup';

import streets_of_tarkov_map_data from './map_data/streets_of_tarkov_map_data.json';
import customs_loot_map_data from './map_data/customs_loot_map_data.json';
import woods_map_data from './map_data/woods_map_data.json';
import lighthouse_loot_map_data from './map_data/lighthouse_loot_map_data.json';
import shoreline_map_data from './map_data/shoreline_map_data.json';
import interchange_map_data from './map_data/interchange_map_modified_data.json';
import reserve_map_data from './map_data/reserve_map_data.json';
import laboratory_loot_map_data from './map_data/laboratory_loot_map_modified_data.json';
import factory_map_data from './map_data/factory_map_data.json';

let websocket;

let extent = [0, 0, 6539, 4394];
let viewExtent = [-200, -200, 6739, 4594];

const customProjection = new Projection({
  code: 'xkcd-image',
  units: 'pixels',
  extent: extent,
});

const popup = new Popup();

let map;
let mapView;
let mapOverlayImage;
let playerMarker;
let currentlyLoadedMap;
let playerVectorLayer;
let playerIconFeature;
let airdropVectorLayer;
let airdropVectorSource;
let airdropFeatures = [];
let questVectorLayer;
let questVectorSource;
let questFeatures = [];
let questPopoverOverlay;
let selectedFeature;

// const selected = new Style({
//   fill: new Fill({
//     color: '#eeeeee',
//   }),
//   stroke: new Stroke({
//     color: 'rgba(255, 255, 255, 0.7)',
//     width: 2,
//   }),
// });

const airdropIconStyle = new Style({
  image: new Icon({
    anchor: [0.5, 0.5],
    anchorXUnits: 'fraction',
    anchorYUnits: 'fraction',
    src: '/images/airdrop.png',
    scale: 0.5
    // width: 10,
    // height: 10
  })
});

const questIconStyle = new Style({
  image: new Icon({
    anchor: [0.5, 0.5],
    anchorXUnits: 'fraction',
    anchorYUnits: 'fraction',
    src: '/images/check-mark.png',
    scale: 0.5
    // width: 10,
    // height: 10
  })
});

let shouldFollowPlayer = false;

let activeRaidCounter = 0;
let lastGameMap = "";
let lastGameRot = 0;
let lastGamePosX = 0;
let lastGamePosZ = 0;
let lastGamePosY = 0;
let lastAirdrops = [];
let lastQuests = [];
let activeQuests = [];

const gameMapNamesDict = {
  "bigmap": customs_loot_map_data,
  "TarkovStreets": streets_of_tarkov_map_data,
  "Woods": woods_map_data,
  "Lighthouse": lighthouse_loot_map_data,
  "Shoreline": shoreline_map_data,
  "Interchange": interchange_map_data,
  "RezervBase": reserve_map_data,
  "laboratory": laboratory_loot_map_data,
  "factory4_day": factory_map_data,
  "factory4_night": factory_map_data
};

function init() {
  console.log("init() called");
  const mousePositionControl = new MousePosition({
    projection: customProjection,
  });

  map = new Map({
    target: 'map',
    controls: defaultControls().extend([new FollowPlayerControl()]), // mousePositionControl
    interactions: defaultInteractions({altShiftDragRotate:false, pinchRotate:false}),
    view: new View({
      projection: customProjection,
      center: getCenter(viewExtent),
      zoom: 1,
      extent: viewExtent,
    })
  });

  map.addOverlay(popup);

  map.on('click', function(event) {
    var point = event.coordinate;

    console.log(`${lastGamePosX} ${point[0]} ${lastGamePosZ} ${point[1]}`);

    console.log("Last Game Data:", lastGameMap, lastGameRot, lastGamePosX, lastGamePosZ, lastGamePosY);

    const features = map.getFeaturesAtPixel(event.pixel, {
      layerFilter: (layer) => layer === questVectorLayer
    });
    
    if (features.length > 0) {
      popup.show(event.coordinate, `<b>${features[0].questName}</b></br>${features[0].questDescription}`);
    } else {
      popup.hide();
    }
  });

  // Player marker stuff
  playerIconFeature = new Feature({
    geometry: new Point([0, 0]),
  });
  
  const playerIconStyle = new Style({
    image: new Icon({
      anchor: [0.5, 0.5],
      anchorXUnits: 'fraction',
      anchorYUnits: 'fraction',
      src: '/images/plain-arrow.png',
      scale: 0.5
      // width: 10,
      // height: 10
    }),
  });
  
  playerIconFeature.setStyle(playerIconStyle);
  
  const playerVectorSource = new VectorSource({
    features: [playerIconFeature],
  });
  
  playerVectorLayer = new VectorLayer({
    source: playerVectorSource,
  });

  playerVectorLayer.setZIndex(99);

  map.addLayer(playerVectorLayer);

  // Airdrop marker stuff  
  airdropVectorSource = new VectorSource({
    features: [],
  });
  
  airdropVectorLayer = new VectorLayer({
    source: airdropVectorSource,
  });

  airdropVectorLayer.setZIndex(99);

  map.addLayer(airdropVectorLayer);

  // Quest marker stuff  
  questVectorSource = new VectorSource({
    features: [],
  });

  questVectorLayer = new VectorLayer({
    source: questVectorSource,
  });

  questVectorLayer.setZIndex(99);

  map.addLayer(questVectorLayer);

  // Finally attempt to connect
  doConnect();

  mapOverlayImage = new ImageLayer({
    source: new Static({
      url: `/maps/enter_a_raid.png`,
      projection: customProjection,
      imageExtent: [0, 0, 600, 400],
    }),
  }),

  map.addLayer(mapOverlayImage);

  mapView = new View({
    projection: customProjection,
    center: [-100, -100, 700, 500],
    showFullExtent: true,
    zoom: 3,
    extent: [0, 0, 600, 400], //viewExtent,
    rotation: 0,
  });

  map.setView(mapView);
}

function changeMap(mapName) {
  console.log("Changing map to:", mapName);

  if (mapOverlayImage) {
    map.removeLayer(mapOverlayImage);
  }

  mapOverlayImage = new ImageLayer({
    source: new Static({
      url: `/maps/${gameMapNamesDict[mapName].MapImageFile}`,
      projection: customProjection,
      imageExtent: gameMapNamesDict[mapName].bounds,
    }),
  }),

  map.addLayer(mapOverlayImage);

  viewExtent = [gameMapNamesDict[mapName].bounds[2] * -0.1, gameMapNamesDict[mapName].bounds[2] * -0.1, gameMapNamesDict[mapName].bounds[2] * 1.1, gameMapNamesDict[mapName].bounds[3] * 1.1]; // TODO: Instead of a fixed increase, multiply the normal bound size by ~1.1x

  mapView = new View({
    projection: customProjection,
    center: getCenter(viewExtent),
    showFullExtent: true,
    zoom: gameMapNamesDict[mapName].initialZoom,
    extent: [0, 0, gameMapNamesDict[mapName].bounds[2], gameMapNamesDict[mapName].bounds[3]], //viewExtent,
    rotation: gameMapNamesDict[mapName].MapRotation * (Math.PI / 180),
  });

  mapView.fit(viewExtent, map.getSize()); 

  map.setView(mapView);
  

  currentlyLoadedMap = mapName;
}

function addAirdropIcon(x, z) {
  const newAirdropFeature = new Feature({
    geometry: new Point([x, z]),
  });

  newAirdropFeature.setStyle(airdropIconStyle);

  airdropVectorSource.addFeature(newAirdropFeature);

  airdropFeatures.push(newAirdropFeature);
}

function addQuestIcon(x, z, name, description) {
  const newQuestFeature = new Feature({
    geometry: new Point([x, z]),
  });

  newQuestFeature.setStyle(questIconStyle);

  newQuestFeature.questName = name;
  newQuestFeature.questDescription = description;

  questVectorSource.addFeature(newQuestFeature);

  questFeatures.push(newQuestFeature);
}

function doConnect() {
  websocket = new WebSocket("ws://" + location.host + "/")
  websocket.onopen = function(evt) { onOpen(evt) }
  websocket.onclose = function(evt) { onClose(evt) }
  websocket.onmessage = function (evt) { onMessage(evt) }
  websocket.onerror = function (evt) { onError(evt) }
}

function onMessage(evt) {
  let incomingMessageJSON = JSON.parse(evt.data);

  lastGameMap = incomingMessageJSON.mapName;
  lastGameRot = incomingMessageJSON.playerRotationX;
  lastGamePosX = incomingMessageJSON.playerPositionX;
  lastGamePosZ = incomingMessageJSON.playerPositionZ;
  lastGamePosY = incomingMessageJSON.playerPositionY;
  lastQuests = incomingMessageJSON.quests;

  if (incomingMessageJSON.quests.length === 0 && questFeatures.length > 0) {
    // Remove the old quest icons as they might be disabled
    questFeatures.forEach(item => {
      questVectorSource.removeFeature(item);
    });
  }

  // Quests
  if (activeRaidCounter < incomingMessageJSON.raidCounter && lastGameMap != "factory4_day" && lastGameMap != "factory4_night") {
    // Remove the old quest icons
    questFeatures.forEach(item => {
      questVectorSource.removeFeature(item);
    });

    activeQuests = lastQuests;

    // Add the new ones
    activeQuests.forEach(item => {
      let x = calculatePolynomialValue(item.Where.x, gameMapNamesDict[lastGameMap].XCoefficients);
      let z = calculatePolynomialValue(item.Where.z, gameMapNamesDict[lastGameMap].ZCoefficients);

      addQuestIcon(x, z, item.NameText, item.DescriptionText);
    });
  }

  // Airdrops
  if (activeRaidCounter < incomingMessageJSON.raidCounter) {
    airdropFeatures.forEach(item => {
      airdropVectorSource.removeFeature(item);
    });

    airdropFeatures = [];
  }
  
  if (airdropFeatures.length < incomingMessageJSON.airdrops.length) {
    const difference = incomingMessageJSON.airdrops.filter((element) => !airdropFeatures.includes(element));

    difference.forEach(airdrop => {
      let x = calculatePolynomialValue(airdrop.x, gameMapNamesDict[lastGameMap].XCoefficients);
      let z = calculatePolynomialValue(airdrop.z, gameMapNamesDict[lastGameMap].ZCoefficients);

      addAirdropIcon(x, z);
    });
  }

  if (currentlyLoadedMap !== lastGameMap) {
    changeMap(lastGameMap);
  }

  let x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].XCoefficients);
  let z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].ZCoefficients);

  if (lastGameMap == "Interchange") {
    // Main mall bounding box + Goshan extension
    if ((lastGamePosX < 83 && lastGamePosX > -157.8 && lastGamePosZ < 193.2 && lastGamePosZ > -303.87) || (lastGamePosX < -157.8 && lastGamePosX > -183.4 && lastGamePosZ < 69 && lastGamePosZ > -178.66)) {
      if (lastGamePosY < 23) { // Parking garage
        x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].ParkingGarageXCoefficients);
        z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].ParkingGarageZCoefficients);
      } else if (lastGamePosY > 32) { // Floor 2
        x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].InteriorFloor2XCoefficients);
        z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].InteriorFloor2ZCoefficients);
      }
    }
  } else if (lastGameMap == "laboratory") {
    if (lastGamePosY > 3) {
      x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].Floor2XCoefficients);
      z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].Floor2ZCoefficients);
    } else if (lastGamePosY < -2) {
      x = calculatePolynomialValue(lastGamePosX, gameMapNamesDict[lastGameMap].TechnicalLevelXCoefficients);
      z = calculatePolynomialValue(lastGamePosZ, gameMapNamesDict[lastGameMap].TechnicalLevelZCoefficients);
    }
  } else if (lastGameMap == "factory4_day" || lastGameMap == "factory4_night") {
    // This map doesn't work
    x = 0;
    z = 0;
  }

  // Move the player marker
  playerIconFeature.getGeometry().setCoordinates([x, z]);
  playerIconFeature.getStyle().getImage().setRotation((gameMapNamesDict[lastGameMap].MapRotation + lastGameRot) * (Math.PI / 180));

  if (shouldFollowPlayer) mapView.setCenter([x, z]);

  activeRaidCounter = incomingMessageJSON.raidCounter;
}

function onOpen(evt) {
  console.log("Opened websocket");
}

function onClose(evt) {
  console.log("Websocket closed");
}

function onError(evt) {
  console.error(evt);
  websocket.close();
}

function calculatePolynomialValue(x, coefficients) {
  let result = 0;

  result = coefficients[0];

  result += coefficients[1] * x;

  return result;
}

class FollowPlayerControl extends Control {
  /**
   * @param {Object} [opt_options] Control options.
   */
  constructor(opt_options) {
    const options = opt_options || {};

    const button = document.createElement('button');
    button.innerHTML = '🧭';

    const element = document.createElement('div');
    element.className = 'follow-player ol-unselectable ol-control';
    element.appendChild(button);

    super({
      element: element,
      target: options.target,
    });

    button.addEventListener('click', this.toggleShouldFollowPlayer.bind(this), false);
  }

  toggleShouldFollowPlayer() {
    shouldFollowPlayer = !shouldFollowPlayer;
    // mapView.setZoom(5);
  }
}

// window.addEventListener("load", init, false)
window.init = init;