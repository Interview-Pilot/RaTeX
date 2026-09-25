use ratex_layout::{layout, to_display_list, LayoutOptions};
use ratex_parser::parser::parse;

fn render(source: &str) -> ratex_layout::LayoutBox {
    layout(&parse(source).unwrap(), &LayoutOptions::default())
}

#[test]
fn slashed_accepts_token_arguments_and_compositions() {
    for (braced, token) in [
        (r"\slashed{p}", r"\slashed p"),
        (r"\slashed{\partial}", r"\slashed\partial"),
    ] {
        assert_eq!(
            serde_json::to_value(to_display_list(&render(braced))).unwrap(),
            serde_json::to_value(to_display_list(&render(token))).unwrap()
        );
    }
    for source in [
        r"\slashed{p}_\mu",
        r"\slashed{p}^{\,2}",
        r"\slashed{\mathbf{p}}",
        r"\frac{1}{\slashed{p}-m}",
        r"(i\slashed{\partial}-m)\psi=0",
    ] {
        let output = render(source);
        assert!(output.width.is_finite() && output.width > 0.0);
        assert!(output.height.is_finite() && output.height > 0.0);
    }
    assert!(parse(r"\slashed").is_err());
    assert!(parse(r"\text{\slashed p}").is_err());
}

#[test]
fn slashed_keeps_body_advance_and_includes_slash_extents() {
    let p = render("p");
    let slash = render("/");
    let slashed = render(r"\slashed{p}");
    assert!((slashed.width - p.width.max(slash.width)).abs() < 1e-10);
    assert!((slashed.height - slashed.depth - p.height + p.depth).abs() < 1e-10);
    assert!((slashed.height + slashed.depth - slash.height - slash.depth).abs() < 1e-10);
    // A narrow subject must still reserve the full slash width.
    assert!((render(r"\slashed{i}").width - slash.width).abs() < 1e-10);
}

#[test]
fn slashed_box_dimensions_match_the_original_tex_package() {
    // Independently measured with Tectonic 0.15.0, tlextras-2022.0r0,
    // amsmath + slashed.sty, using \setbox0=\hbox{$\displaystyle ...$}
    // and \the\wd0 / \the\ht0 / \the\dp0 at 10pt. Compare geometry
    // separately from pixel scores, which also reflect CM vs KaTeX outlines.
    for (source, width, height, depth) in [
        (r"\slashed{p}", 5.03125, 6.18056, 3.81944),
        (r"\slashed{D}", 8.55695, 8.41666, 1.58334),
        (r"\slashed{\partial}", 5.86458, 8.47221, 1.52779),
        (r"\slashed{\mathbf{p}}", 6.38885, 6.25, 3.75),
    ] {
        let actual = render(source);
        for (actual, tex_pt) in [
            (actual.width, width),
            (actual.height, height),
            (actual.depth, depth),
        ] {
            assert!(
                (actual - tex_pt / 10.0).abs() < 0.00001,
                "{source}: {actual} vs {tex_pt}pt"
            );
        }
    }
}

#[test]
fn slashed_optical_corrections_and_style_scaling_are_preserved() {
    use ratex_types::display_item::DisplayItem;

    // TeX's centered slash plus its built-in D and partial corrections.
    for (source, slash_x) in [
        (r"\slashed{D}", 0.2463031),
        (r"\slashed{\partial}", 0.1018748),
    ] {
        let display = to_display_list(&render(source));
        assert!(
            matches!(&display.items[0], DisplayItem::GlyphPath { x, char_code: 47, .. }
            if (*x - slash_x).abs() < 0.00001)
        );
    }
    let display = to_display_list(&render(r"\slashed{f}"));
    assert!(matches!(
        &display.items[0],
        DisplayItem::GlyphPath {
            char_code: 0xE020,
            ..
        }
    ));
    for (style, scale) in [(r"\scriptstyle", 0.7), (r"\scriptscriptstyle", 0.5)] {
        let full = render(r"\slashed{p}");
        let small = render(&format!(r"{style}\slashed{{p}}"));
        assert!((small.width - full.width * scale).abs() < 1e-10);
        assert!((small.height - full.height * scale).abs() < 1e-10);
        assert!((small.depth - full.depth * scale).abs() < 1e-10);
        for item in &to_display_list(&small).items {
            assert!(
                matches!(item, DisplayItem::GlyphPath { scale: actual, .. } if (*actual - scale).abs() < 1e-10)
            );
        }
    }
}

#[test]
fn slashed_depth_uses_the_shared_budget() {
    let nested = |depth| format!("{}p{}", r"\slashed{".repeat(depth), "}".repeat(depth));
    assert!(parse(&nested(32)).is_ok());
    for source in [nested(33), nested(300), format!("{{{}}}", nested(32))] {
        let error = parse(&source).unwrap_err().to_string();
        assert!(error.contains("Recursion limit exceeded"), "{error}");
    }
}
