// 120 x 120 x 95 mm removable-panel enclosure
// Designed for FDM printing. Dimensions in mm.

part = "body"; // "body", "top", "bottom", "assembly"

outer_x = 120;
outer_y = 120;
total_h = 95;
wall = 4;
panel_t = 4;
body_h = total_h - 2*panel_t; // 87 mm
corner_r = 8;
inner_r = corner_r - wall;

// M2 fastening
screw_clearance_d = 2.2;
pilot_d = 1.65;        // suitable starting point for M2 self-tapping into printed plastic
boss_d = 9;
boss_offset = 8;

// Ventilation pattern
vent_d = 6;
vent_pitch_x = 16;
vent_pitch_z = 15;
vent_edge_x = 18;
vent_edge_z = 14;

$fn = 20;

module rounded_rect_2d(w, d, r) {
    hull() {
        for (x=[r, w-r], y=[r, d-r])
            translate([x,y]) circle(r=r);
    }
}

module rounded_prism(w, d, h, r) {
    linear_extrude(height=h)
        rounded_rect_2d(w,d,r);
}

module boss_with_pilot(z0=0, h=body_h) {
    difference() {
        cylinder(d=boss_d, h=h);
        translate([0,0,-0.1]) cylinder(d=pilot_d, h=h+0.2);
    }
}

module body_shell() {
    difference() {
        // Main rounded shell
        difference() {
            rounded_prism(outer_x, outer_y, body_h, corner_r);
            translate([wall, wall, -0.1])
                rounded_prism(outer_x-2*wall, outer_y-2*wall, body_h+0.2, inner_r);
        }

        // Vent holes through front/back walls (Y direction)
        for (x=[vent_edge_x:vent_pitch_x:outer_x-vent_edge_x])
            for (z=[vent_edge_z:vent_pitch_z:body_h-vent_edge_z]) {
                translate([x,-1,z]) rotate([-90,0,0]) cylinder(d=vent_d,h=wall+2);
                translate([x,outer_y-wall-1,z]) rotate([-90,0,0]) cylinder(d=vent_d,h=wall+2);
            }

        // Vent holes through left/right walls (X direction)
        for (y=[vent_edge_x:vent_pitch_x:outer_y-vent_edge_x])
            for (z=[vent_edge_z:vent_pitch_z:body_h-vent_edge_z]) {
                translate([-1,y,z]) rotate([0,90,0]) cylinder(d=vent_d,h=wall+2);
                translate([outer_x-wall-1,y,z]) rotate([0,90,0]) cylinder(d=vent_d,h=wall+2);
            }
    }

    // Four internal screw bosses
    for (x=[boss_offset, outer_x-boss_offset], y=[boss_offset, outer_y-boss_offset])
        translate([x,y,0]) boss_with_pilot();
}

module panel() {
    difference() {
        rounded_prism(outer_x, outer_y, panel_t, corner_r);
        for (x=[boss_offset, outer_x-boss_offset], y=[boss_offset, outer_y-boss_offset])
            translate([x,y,-0.1]) cylinder(d=screw_clearance_d,h=panel_t+0.2);
    }
}

module assembly(explode=3) {
    color("gainsboro") translate([0,0,0]) panel();
    color("silver") translate([0,0,panel_t+explode]) body_shell();
    color("gainsboro") translate([0,0,panel_t+explode+body_h+explode]) panel();
}

if (part == "body") body_shell();
else if (part == "top") panel();
else if (part == "bottom") panel();
else assembly();
