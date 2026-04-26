/*
    Seeds reference_data."Countries" using Flagpedia country codes and flag URLs,
    and REST Countries dialing codes.
    Sources:
    - https://flagpedia.net/download/api
    - https://restcountries.com/

    CallingCode stores one canonical country calling code per country.
    The canonical value is the source root code.
*/

INSERT INTO reference_data."Countries" ("Id", "ShortCode", "Name", "CallingCode", "FlagUrl", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
VALUES
    (gen_random_uuid(), 'AD', 'Andorra', '+3', 'https://flagcdn.com/ad.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AE', 'United Arab Emirates', '+9', 'https://flagcdn.com/ae.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AF', 'Afghanistan', '+9', 'https://flagcdn.com/af.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AG', 'Antigua and Barbuda', '+1', 'https://flagcdn.com/ag.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AI', 'Anguilla', '+1', 'https://flagcdn.com/ai.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AL', 'Albania', '+3', 'https://flagcdn.com/al.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AM', 'Armenia', '+3', 'https://flagcdn.com/am.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AO', 'Angola', '+2', 'https://flagcdn.com/ao.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AQ', 'Antarctica', '', 'https://flagcdn.com/aq.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AR', 'Argentina', '+5', 'https://flagcdn.com/ar.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AS', 'American Samoa', '+1', 'https://flagcdn.com/as.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AT', 'Austria', '+4', 'https://flagcdn.com/at.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AU', 'Australia', '+6', 'https://flagcdn.com/au.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AW', 'Aruba', '+2', 'https://flagcdn.com/aw.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AX', 'Åland Islands', '+3', 'https://flagcdn.com/ax.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'AZ', 'Azerbaijan', '+9', 'https://flagcdn.com/az.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BA', 'Bosnia and Herzegovina', '+3', 'https://flagcdn.com/ba.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BB', 'Barbados', '+1', 'https://flagcdn.com/bb.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BD', 'Bangladesh', '+8', 'https://flagcdn.com/bd.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BE', 'Belgium', '+3', 'https://flagcdn.com/be.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BF', 'Burkina Faso', '+2', 'https://flagcdn.com/bf.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BG', 'Bulgaria', '+3', 'https://flagcdn.com/bg.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BH', 'Bahrain', '+9', 'https://flagcdn.com/bh.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BI', 'Burundi', '+2', 'https://flagcdn.com/bi.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BJ', 'Benin', '+2', 'https://flagcdn.com/bj.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BL', 'Saint Barthélemy', '+5', 'https://flagcdn.com/bl.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BM', 'Bermuda', '+1', 'https://flagcdn.com/bm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BN', 'Brunei', '+6', 'https://flagcdn.com/bn.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BO', 'Bolivia', '+5', 'https://flagcdn.com/bo.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BQ', 'Caribbean Netherlands', '+5', 'https://flagcdn.com/bq.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BR', 'Brazil', '+5', 'https://flagcdn.com/br.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BS', 'Bahamas', '+1', 'https://flagcdn.com/bs.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BT', 'Bhutan', '+9', 'https://flagcdn.com/bt.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BV', 'Bouvet Island', '+4', 'https://flagcdn.com/bv.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BW', 'Botswana', '+2', 'https://flagcdn.com/bw.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BY', 'Belarus', '+3', 'https://flagcdn.com/by.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'BZ', 'Belize', '+5', 'https://flagcdn.com/bz.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CA', 'Canada', '+1', 'https://flagcdn.com/ca.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CC', 'Cocos (Keeling) Islands', '+6', 'https://flagcdn.com/cc.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CD', 'DR Congo', '+2', 'https://flagcdn.com/cd.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CF', 'Central African Republic', '+2', 'https://flagcdn.com/cf.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CG', 'Republic of the Congo', '+2', 'https://flagcdn.com/cg.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CH', 'Switzerland', '+4', 'https://flagcdn.com/ch.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CI', 'Côte d''Ivoire (Ivory Coast)', '+2', 'https://flagcdn.com/ci.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CK', 'Cook Islands', '+6', 'https://flagcdn.com/ck.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CL', 'Chile', '+5', 'https://flagcdn.com/cl.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CM', 'Cameroon', '+2', 'https://flagcdn.com/cm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CN', 'China', '+8', 'https://flagcdn.com/cn.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CO', 'Colombia', '+5', 'https://flagcdn.com/co.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CR', 'Costa Rica', '+5', 'https://flagcdn.com/cr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CU', 'Cuba', '+5', 'https://flagcdn.com/cu.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CV', 'Cape Verde', '+2', 'https://flagcdn.com/cv.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CW', 'Curaçao', '+5', 'https://flagcdn.com/cw.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CX', 'Christmas Island', '+6', 'https://flagcdn.com/cx.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CY', 'Cyprus', '+3', 'https://flagcdn.com/cy.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'CZ', 'Czechia', '+4', 'https://flagcdn.com/cz.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'DE', 'Germany', '+4', 'https://flagcdn.com/de.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'DJ', 'Djibouti', '+2', 'https://flagcdn.com/dj.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'DK', 'Denmark', '+4', 'https://flagcdn.com/dk.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'DM', 'Dominica', '+1', 'https://flagcdn.com/dm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'DO', 'Dominican Republic', '+1', 'https://flagcdn.com/do.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'DZ', 'Algeria', '+2', 'https://flagcdn.com/dz.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'EC', 'Ecuador', '+5', 'https://flagcdn.com/ec.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'EE', 'Estonia', '+3', 'https://flagcdn.com/ee.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'EG', 'Egypt', '+2', 'https://flagcdn.com/eg.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'EH', 'Western Sahara', '+2', 'https://flagcdn.com/eh.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'ER', 'Eritrea', '+2', 'https://flagcdn.com/er.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'ES', 'Spain', '+3', 'https://flagcdn.com/es.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'ET', 'Ethiopia', '+2', 'https://flagcdn.com/et.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'FI', 'Finland', '+3', 'https://flagcdn.com/fi.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'FJ', 'Fiji', '+6', 'https://flagcdn.com/fj.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'FK', 'Falkland Islands', '+5', 'https://flagcdn.com/fk.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'FM', 'Micronesia', '+6', 'https://flagcdn.com/fm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'FO', 'Faroe Islands', '+2', 'https://flagcdn.com/fo.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'FR', 'France', '+3', 'https://flagcdn.com/fr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GA', 'Gabon', '+2', 'https://flagcdn.com/ga.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GB', 'United Kingdom', '+4', 'https://flagcdn.com/gb.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GD', 'Grenada', '+1', 'https://flagcdn.com/gd.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GE', 'Georgia', '+9', 'https://flagcdn.com/ge.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GF', 'French Guiana', '+5', 'https://flagcdn.com/gf.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GG', 'Guernsey', '+4', 'https://flagcdn.com/gg.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GH', 'Ghana', '+2', 'https://flagcdn.com/gh.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GI', 'Gibraltar', '+3', 'https://flagcdn.com/gi.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GL', 'Greenland', '+2', 'https://flagcdn.com/gl.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GM', 'Gambia', '+2', 'https://flagcdn.com/gm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GN', 'Guinea', '+2', 'https://flagcdn.com/gn.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GP', 'Guadeloupe', '+5', 'https://flagcdn.com/gp.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GQ', 'Equatorial Guinea', '+2', 'https://flagcdn.com/gq.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GR', 'Greece', '+3', 'https://flagcdn.com/gr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GS', 'South Georgia', '+5', 'https://flagcdn.com/gs.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GT', 'Guatemala', '+5', 'https://flagcdn.com/gt.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GU', 'Guam', '+1', 'https://flagcdn.com/gu.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GW', 'Guinea-Bissau', '+2', 'https://flagcdn.com/gw.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'GY', 'Guyana', '+5', 'https://flagcdn.com/gy.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'HK', 'Hong Kong', '+8', 'https://flagcdn.com/hk.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'HM', 'Heard Island and McDonald Islands', '', 'https://flagcdn.com/hm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'HN', 'Honduras', '+5', 'https://flagcdn.com/hn.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'HR', 'Croatia', '+3', 'https://flagcdn.com/hr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'HT', 'Haiti', '+5', 'https://flagcdn.com/ht.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'HU', 'Hungary', '+3', 'https://flagcdn.com/hu.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'ID', 'Indonesia', '+6', 'https://flagcdn.com/id.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'IE', 'Ireland', '+3', 'https://flagcdn.com/ie.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'IL', 'Israel', '+9', 'https://flagcdn.com/il.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'IM', 'Isle of Man', '+4', 'https://flagcdn.com/im.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'IN', 'India', '+9', 'https://flagcdn.com/in.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'IO', 'British Indian Ocean Territory', '+2', 'https://flagcdn.com/io.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'IQ', 'Iraq', '+9', 'https://flagcdn.com/iq.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'IR', 'Iran', '+9', 'https://flagcdn.com/ir.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'IS', 'Iceland', '+3', 'https://flagcdn.com/is.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'IT', 'Italy', '+3', 'https://flagcdn.com/it.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'JE', 'Jersey', '+4', 'https://flagcdn.com/je.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'JM', 'Jamaica', '+1', 'https://flagcdn.com/jm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'JO', 'Jordan', '+9', 'https://flagcdn.com/jo.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'JP', 'Japan', '+8', 'https://flagcdn.com/jp.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KE', 'Kenya', '+2', 'https://flagcdn.com/ke.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KG', 'Kyrgyzstan', '+9', 'https://flagcdn.com/kg.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KH', 'Cambodia', '+8', 'https://flagcdn.com/kh.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KI', 'Kiribati', '+6', 'https://flagcdn.com/ki.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KM', 'Comoros', '+2', 'https://flagcdn.com/km.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KN', 'Saint Kitts and Nevis', '+1', 'https://flagcdn.com/kn.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KP', 'North Korea', '+8', 'https://flagcdn.com/kp.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KR', 'South Korea', '+8', 'https://flagcdn.com/kr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KW', 'Kuwait', '+9', 'https://flagcdn.com/kw.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KY', 'Cayman Islands', '+1', 'https://flagcdn.com/ky.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'KZ', 'Kazakhstan', '+7', 'https://flagcdn.com/kz.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LA', 'Laos', '+8', 'https://flagcdn.com/la.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LB', 'Lebanon', '+9', 'https://flagcdn.com/lb.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LC', 'Saint Lucia', '+1', 'https://flagcdn.com/lc.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LI', 'Liechtenstein', '+4', 'https://flagcdn.com/li.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LK', 'Sri Lanka', '+9', 'https://flagcdn.com/lk.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LR', 'Liberia', '+2', 'https://flagcdn.com/lr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LS', 'Lesotho', '+2', 'https://flagcdn.com/ls.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LT', 'Lithuania', '+3', 'https://flagcdn.com/lt.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LU', 'Luxembourg', '+3', 'https://flagcdn.com/lu.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LV', 'Latvia', '+3', 'https://flagcdn.com/lv.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'LY', 'Libya', '+2', 'https://flagcdn.com/ly.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MA', 'Morocco', '+2', 'https://flagcdn.com/ma.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MC', 'Monaco', '+3', 'https://flagcdn.com/mc.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MD', 'Moldova', '+3', 'https://flagcdn.com/md.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'ME', 'Montenegro', '+3', 'https://flagcdn.com/me.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MF', 'Saint Martin', '+5', 'https://flagcdn.com/mf.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MG', 'Madagascar', '+2', 'https://flagcdn.com/mg.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MH', 'Marshall Islands', '+6', 'https://flagcdn.com/mh.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MK', 'North Macedonia', '+3', 'https://flagcdn.com/mk.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'ML', 'Mali', '+2', 'https://flagcdn.com/ml.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MM', 'Myanmar', '+9', 'https://flagcdn.com/mm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MN', 'Mongolia', '+9', 'https://flagcdn.com/mn.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MO', 'Macau', '+8', 'https://flagcdn.com/mo.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MP', 'Northern Mariana Islands', '+1', 'https://flagcdn.com/mp.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MQ', 'Martinique', '+5', 'https://flagcdn.com/mq.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MR', 'Mauritania', '+2', 'https://flagcdn.com/mr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MS', 'Montserrat', '+1', 'https://flagcdn.com/ms.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MT', 'Malta', '+3', 'https://flagcdn.com/mt.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MU', 'Mauritius', '+2', 'https://flagcdn.com/mu.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MV', 'Maldives', '+9', 'https://flagcdn.com/mv.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MW', 'Malawi', '+2', 'https://flagcdn.com/mw.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MX', 'Mexico', '+5', 'https://flagcdn.com/mx.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MY', 'Malaysia', '+6', 'https://flagcdn.com/my.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'MZ', 'Mozambique', '+2', 'https://flagcdn.com/mz.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NA', 'Namibia', '+2', 'https://flagcdn.com/na.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NC', 'New Caledonia', '+6', 'https://flagcdn.com/nc.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NE', 'Niger', '+2', 'https://flagcdn.com/ne.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NF', 'Norfolk Island', '+6', 'https://flagcdn.com/nf.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NG', 'Nigeria', '+2', 'https://flagcdn.com/ng.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NI', 'Nicaragua', '+5', 'https://flagcdn.com/ni.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NL', 'Netherlands', '+3', 'https://flagcdn.com/nl.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NO', 'Norway', '+4', 'https://flagcdn.com/no.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NP', 'Nepal', '+9', 'https://flagcdn.com/np.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NR', 'Nauru', '+6', 'https://flagcdn.com/nr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NU', 'Niue', '+6', 'https://flagcdn.com/nu.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'NZ', 'New Zealand', '+6', 'https://flagcdn.com/nz.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'OM', 'Oman', '+9', 'https://flagcdn.com/om.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PA', 'Panama', '+5', 'https://flagcdn.com/pa.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PE', 'Peru', '+5', 'https://flagcdn.com/pe.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PF', 'French Polynesia', '+6', 'https://flagcdn.com/pf.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PG', 'Papua New Guinea', '+6', 'https://flagcdn.com/pg.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PH', 'Philippines', '+6', 'https://flagcdn.com/ph.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PK', 'Pakistan', '+9', 'https://flagcdn.com/pk.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PL', 'Poland', '+4', 'https://flagcdn.com/pl.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PM', 'Saint Pierre and Miquelon', '+5', 'https://flagcdn.com/pm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PN', 'Pitcairn Islands', '+6', 'https://flagcdn.com/pn.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PR', 'Puerto Rico', '+1', 'https://flagcdn.com/pr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PS', 'Palestine', '+9', 'https://flagcdn.com/ps.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PT', 'Portugal', '+3', 'https://flagcdn.com/pt.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PW', 'Palau', '+6', 'https://flagcdn.com/pw.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'PY', 'Paraguay', '+5', 'https://flagcdn.com/py.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'QA', 'Qatar', '+9', 'https://flagcdn.com/qa.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'RE', 'Réunion', '+2', 'https://flagcdn.com/re.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'RO', 'Romania', '+4', 'https://flagcdn.com/ro.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'RS', 'Serbia', '+3', 'https://flagcdn.com/rs.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'RU', 'Russia', '+7', 'https://flagcdn.com/ru.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'RW', 'Rwanda', '+2', 'https://flagcdn.com/rw.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SA', 'Saudi Arabia', '+9', 'https://flagcdn.com/sa.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SB', 'Solomon Islands', '+6', 'https://flagcdn.com/sb.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SC', 'Seychelles', '+2', 'https://flagcdn.com/sc.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SD', 'Sudan', '+2', 'https://flagcdn.com/sd.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SE', 'Sweden', '+4', 'https://flagcdn.com/se.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SG', 'Singapore', '+6', 'https://flagcdn.com/sg.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SH', 'Saint Helena, Ascension and Tristan da Cunha', '+2', 'https://flagcdn.com/sh.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SI', 'Slovenia', '+3', 'https://flagcdn.com/si.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SJ', 'Svalbard and Jan Mayen', '+4', 'https://flagcdn.com/sj.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SK', 'Slovakia', '+4', 'https://flagcdn.com/sk.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SL', 'Sierra Leone', '+2', 'https://flagcdn.com/sl.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SM', 'San Marino', '+3', 'https://flagcdn.com/sm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SN', 'Senegal', '+2', 'https://flagcdn.com/sn.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SO', 'Somalia', '+2', 'https://flagcdn.com/so.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SR', 'Suriname', '+5', 'https://flagcdn.com/sr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SS', 'South Sudan', '+2', 'https://flagcdn.com/ss.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'ST', 'São Tomé and Príncipe', '+2', 'https://flagcdn.com/st.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SV', 'El Salvador', '+5', 'https://flagcdn.com/sv.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SX', 'Sint Maarten', '+1', 'https://flagcdn.com/sx.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SY', 'Syria', '+9', 'https://flagcdn.com/sy.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'SZ', 'Eswatini (Swaziland)', '+2', 'https://flagcdn.com/sz.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TC', 'Turks and Caicos Islands', '+1', 'https://flagcdn.com/tc.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TD', 'Chad', '+2', 'https://flagcdn.com/td.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TF', 'French Southern and Antarctic Lands', '+2', 'https://flagcdn.com/tf.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TG', 'Togo', '+2', 'https://flagcdn.com/tg.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TH', 'Thailand', '+6', 'https://flagcdn.com/th.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TJ', 'Tajikistan', '+9', 'https://flagcdn.com/tj.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TK', 'Tokelau', '+6', 'https://flagcdn.com/tk.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TL', 'Timor-Leste', '+6', 'https://flagcdn.com/tl.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TM', 'Turkmenistan', '+9', 'https://flagcdn.com/tm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TN', 'Tunisia', '+2', 'https://flagcdn.com/tn.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TO', 'Tonga', '+6', 'https://flagcdn.com/to.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TR', 'Turkey', '+9', 'https://flagcdn.com/tr.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TT', 'Trinidad and Tobago', '+1', 'https://flagcdn.com/tt.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TV', 'Tuvalu', '+6', 'https://flagcdn.com/tv.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TW', 'Taiwan', '+8', 'https://flagcdn.com/tw.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'TZ', 'Tanzania', '+2', 'https://flagcdn.com/tz.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'UA', 'Ukraine', '+3', 'https://flagcdn.com/ua.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'UG', 'Uganda', '+2', 'https://flagcdn.com/ug.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'UM', 'United States Minor Outlying Islands', '+2', 'https://flagcdn.com/um.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'US', 'United States', '+1', 'https://flagcdn.com/us.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'UY', 'Uruguay', '+5', 'https://flagcdn.com/uy.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'UZ', 'Uzbekistan', '+9', 'https://flagcdn.com/uz.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'VA', 'Vatican City (Holy See)', '+3', 'https://flagcdn.com/va.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'VC', 'Saint Vincent and the Grenadines', '+1', 'https://flagcdn.com/vc.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'VE', 'Venezuela', '+5', 'https://flagcdn.com/ve.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'VG', 'British Virgin Islands', '+1', 'https://flagcdn.com/vg.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'VI', 'United States Virgin Islands', '+1', 'https://flagcdn.com/vi.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'VN', 'Vietnam', '+8', 'https://flagcdn.com/vn.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'VU', 'Vanuatu', '+6', 'https://flagcdn.com/vu.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'WF', 'Wallis and Futuna', '+6', 'https://flagcdn.com/wf.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'WS', 'Samoa', '+6', 'https://flagcdn.com/ws.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'XK', 'Kosovo', '+3', 'https://flagcdn.com/xk.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'YE', 'Yemen', '+9', 'https://flagcdn.com/ye.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'YT', 'Mayotte', '+2', 'https://flagcdn.com/yt.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'ZA', 'South Africa', '+2', 'https://flagcdn.com/za.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'ZM', 'Zambia', '+2', 'https://flagcdn.com/zm.svg', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'ZW', 'Zimbabwe', '+2', 'https://flagcdn.com/zw.svg', NOW(), NOW(), FALSE)
ON CONFLICT ("ShortCode") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "CallingCode" = EXCLUDED."CallingCode",
    "FlagUrl" = EXCLUDED."FlagUrl",
    "UpdatedAtUtc" = NOW()
WHERE reference_data."Countries"."Name" IS DISTINCT FROM EXCLUDED."Name"
   OR reference_data."Countries"."CallingCode" IS DISTINCT FROM EXCLUDED."CallingCode"
   OR reference_data."Countries"."FlagUrl" IS DISTINCT FROM EXCLUDED."FlagUrl";

UPDATE infrastructure."Providers"
SET
    "IsActive" = FALSE,
    "UpdatedAtUtc" = NOW()
WHERE "ProviderType" = 1
    AND "ProviderKey" <> 'mailtrap'
    AND "IsActive" = TRUE;

UPDATE infrastructure."Providers"
SET
    "IsActive" = FALSE,
    "IsDeleted" = TRUE,
    "DeletedAtUtc" = NOW(),
    "DeletedBy" = 'system',
    "UpdatedAtUtc" = NOW()
WHERE "ProviderType" = 1
    AND "ProviderKey" = 'logging'
    AND "IsDeleted" = FALSE;

INSERT INTO infrastructure."Providers" ("Id", "ProviderType", "ProviderName", "ProviderKey", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
VALUES
    (gen_random_uuid(), 1, 'Mailtrap', 'mailtrap', TRUE, NOW(), NOW(), FALSE),
    (gen_random_uuid(), 2, 'Noop (Stub)', 'noop', TRUE, NOW(), NOW(), FALSE),
    (gen_random_uuid(), 2, 'Cloudflare R2', 'cloudflare-r2', FALSE, NOW(), NOW(), FALSE)
ON CONFLICT ("ProviderType", "ProviderKey") DO UPDATE SET
    "ProviderName" = EXCLUDED."ProviderName",
    "IsActive" = EXCLUDED."IsActive",
    "UpdatedAtUtc" = NOW()
WHERE infrastructure."Providers"."ProviderName" IS DISTINCT FROM EXCLUDED."ProviderName"
   OR infrastructure."Providers"."IsActive" IS DISTINCT FROM EXCLUDED."IsActive";

INSERT INTO payments."PaymentProviders" ("Id", "ProviderName", "ProviderKey", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
VALUES
    (gen_random_uuid(), 'SafeHaven', 'safehaven', TRUE, NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'Credo', 'credo', TRUE, NOW(), NOW(), FALSE)
ON CONFLICT ("ProviderKey") DO UPDATE SET
    "ProviderName" = EXCLUDED."ProviderName",
    "IsActive" = EXCLUDED."IsActive",
    "UpdatedAtUtc" = NOW()
WHERE payments."PaymentProviders"."ProviderName" IS DISTINCT FROM EXCLUDED."ProviderName"
   OR payments."PaymentProviders"."IsActive" IS DISTINCT FROM EXCLUDED."IsActive";

INSERT INTO payments."Currencies" ("Id", "CurrencyCode", "CurrencyName", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
VALUES
    (gen_random_uuid(), 'NGN', 'Naira', TRUE, NOW(), NOW(), FALSE),
    (gen_random_uuid(), 'USD', 'US Dollar', TRUE, NOW(), NOW(), FALSE)
ON CONFLICT ("CurrencyCode") DO UPDATE SET
    "CurrencyName" = EXCLUDED."CurrencyName",
    "IsActive" = EXCLUDED."IsActive",
    "UpdatedAtUtc" = NOW()
WHERE payments."Currencies"."CurrencyName" IS DISTINCT FROM EXCLUDED."CurrencyName"
   OR payments."Currencies"."IsActive" IS DISTINCT FROM EXCLUDED."IsActive";

WITH country_currency_source AS (
    SELECT
        gen_random_uuid() AS id,
        c."Id" AS country_id,
        cu."Id" AS currency_id,
        TRUE AS is_default,
        TRUE AS is_active
    FROM reference_data."Countries" c
    INNER JOIN payments."Currencies" cu
        ON cu."CurrencyCode" =
            CASE
                WHEN c."ShortCode" = 'NG' THEN 'NGN'
                ELSE 'USD'
            END
)
INSERT INTO payments."CountryCurrencies" ("Id", "CountryId", "CurrencyId", "IsDefault", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT id, country_id, currency_id, is_default, is_active, NOW(), NOW(), FALSE
FROM country_currency_source
ON CONFLICT ("CountryId", "CurrencyId") DO UPDATE SET
    "IsDefault" = EXCLUDED."IsDefault",
    "IsActive" = EXCLUDED."IsActive",
    "UpdatedAtUtc" = NOW()
WHERE payments."CountryCurrencies"."IsDefault" IS DISTINCT FROM EXCLUDED."IsDefault"
   OR payments."CountryCurrencies"."IsActive" IS DISTINCT FROM EXCLUDED."IsActive";

WITH provider_config_source AS (
    SELECT
        pp."Id" AS payment_provider_id,
        c."Id" AS currency_id,
        cfg."PaymentIntent",
        cfg."PaymentMethodType",
        cfg."IsEnabled"
    FROM (VALUES
        ('safehaven', 'NGN', 1, 2, TRUE),
        ('safehaven', 'NGN', 2, 2, TRUE),
        ('credo', 'NGN', 1, 1, TRUE),
        ('credo', 'NGN', 2, 1, TRUE)
    ) AS cfg("ProviderKey", "CurrencyCode", "PaymentIntent", "PaymentMethodType", "IsEnabled")
    INNER JOIN payments."PaymentProviders" pp ON pp."ProviderKey" = cfg."ProviderKey"
    INNER JOIN payments."Currencies" c ON c."CurrencyCode" = cfg."CurrencyCode"
)
INSERT INTO payments."PaymentProviderConfigurations" ("Id", "PaymentProviderId", "CurrencyId", "PaymentIntent", "PaymentMethodType", "IsEnabled", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), payment_provider_id, currency_id, "PaymentIntent", "PaymentMethodType", "IsEnabled", NOW(), NOW(), FALSE
FROM provider_config_source
ON CONFLICT ("PaymentProviderId", "CurrencyId", "PaymentIntent") DO UPDATE SET
    "PaymentMethodType" = EXCLUDED."PaymentMethodType",
    "IsEnabled" = EXCLUDED."IsEnabled",
    "UpdatedAtUtc" = NOW()
WHERE payments."PaymentProviderConfigurations"."PaymentMethodType" IS DISTINCT FROM EXCLUDED."PaymentMethodType"
   OR payments."PaymentProviderConfigurations"."IsEnabled" IS DISTINCT FROM EXCLUDED."IsEnabled";

/*
    Seeds stakeholders."Tenants".

    The default tenant (Guid.Empty) is used as a fallback for brand resolution
    when a specific tenant id does not exist.
*/

INSERT INTO stakeholders."Tenants" ("Id", "Name", "BrandKey", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
VALUES
    ('1203d9d1-2a6b-48ef-9cc1-e561a23aff72'::uuid, 'Default Tenant', 'default', NOW(), NOW(), FALSE)
ON CONFLICT ("Id") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "BrandKey" = EXCLUDED."BrandKey",
    "UpdatedAtUtc" = NOW()
WHERE stakeholders."Tenants"."Name" IS DISTINCT FROM EXCLUDED."Name"
   OR stakeholders."Tenants"."BrandKey" IS DISTINCT FROM EXCLUDED."BrandKey";

/*
    Seeds stakeholders."StakeholderTypes".

    Defines the default stakeholder types available in the system.
*/

INSERT INTO stakeholders."StakeholderTypes" ("Id", "TenantId", "Name", "Key", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
VALUES
    ('65018401-f34e-422a-ad65-ed4b4a5ed266'::uuid, '1203d9d1-2a6b-48ef-9cc1-e561a23aff72'::uuid, 'Customer', 'customer', NOW(), NOW(), FALSE)
ON CONFLICT ("TenantId", "Key") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "UpdatedAtUtc" = NOW()
WHERE stakeholders."StakeholderTypes"."Name" IS DISTINCT FROM EXCLUDED."Name";

/*
    Seeds notifications."EmailNotificationTemplates".

    Subject supports named placeholders in the form {{:PlaceholderName:}}.
    The template file name points to an HTML content fragment loaded from filesystem.
*/

INSERT INTO notifications."EmailNotificationTemplates" ("Id", "NotificationType", "Description", "Subject", "TemplateFileName", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
VALUES
    (gen_random_uuid(), 1, 'Account created notification', 'Welcome to {{:Product:}}', 'AccountCreated.html', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 2, 'Email confirmation OTP notification', 'Please confirm your email', 'ConfirmEmail.html', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 3, 'Reset password OTP notification', 'Reset your password', 'ResetPassword.html', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 4, 'Password reset success notification', 'Your password has been reset', 'PasswordResetSuccessful.html', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 5, 'Email confirmation follow-up notification', 'Reminder to confirm your email', 'EmailConfirmationFollowUp.html', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 6, 'Sign-in successful notification', 'Successful sign-in', 'SignInSuccessful.html', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 7, 'Account locked notification', 'Your account has been locked', 'AccountLocked.html', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 8, 'Trial expired notification', 'Your {{:Product:}} trial has ended', 'TrialExpired.html', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 9, 'Subscription cancelled notification', 'Help us improve {{:Product:}}', 'CancelledSubscription.html', NOW(), NOW(), FALSE),
    (gen_random_uuid(), 10, 'Subscription invoice notification', 'Your invoice from {{:Product:}}', 'Invoice.html', NOW(), NOW(), FALSE)
ON CONFLICT ("NotificationType") DO UPDATE SET
    "Description" = EXCLUDED."Description",
    "Subject" = EXCLUDED."Subject",
    "TemplateFileName" = EXCLUDED."TemplateFileName",
    "UpdatedAtUtc" = NOW()
WHERE notifications."EmailNotificationTemplates"."Description" IS DISTINCT FROM EXCLUDED."Description"
   OR notifications."EmailNotificationTemplates"."Subject" IS DISTINCT FROM EXCLUDED."Subject"
   OR notifications."EmailNotificationTemplates"."TemplateFileName" IS DISTINCT FROM EXCLUDED."TemplateFileName";
